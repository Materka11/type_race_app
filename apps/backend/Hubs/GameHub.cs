using backend.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace backend.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, Game> Rooms = new();
        private static readonly ConcurrentDictionary<string, string> ConnectionToRoomMap = new();

        public async Task JoinGame(string roomId, string name)
        {
            Console.WriteLine($"JoinGame attempt: roomId={roomId}, name={name}, connectionId={Context.ConnectionId}");

            if (Rooms.TryGetValue(roomId, out var game) && game.Players.Values.Any(p => p.Name == name))
            {
                Console.WriteLine($"Rejected: Player with name {name} already exists in room {roomId}");
                await Clients.Caller.SendAsync("error", "A player with this name is already in the room.");
                return;
            }

            ConnectionToRoomMap[Context.ConnectionId] = roomId;

            if (roomId.Length > 50 || name.Length > 50)
            {
                Console.WriteLine($"Rejected: Room ID or name too long for {Context.ConnectionId}");
                await Clients.Caller.SendAsync("error", "Room ID or name too long.");
                return;
            }
            if (!Regex.IsMatch(roomId, @"^[a-zA-Z0-9_-]+$"))
            {
                Console.WriteLine($"Rejected: Invalid characters in room ID for {Context.ConnectionId}");
                await Clients.Caller.SendAsync("error", "Invalid characters in room ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomId))
            {
                Console.WriteLine($"Rejected: Invalid room ID for {Context.ConnectionId}");
                await Clients.Caller.SendAsync("error", "Invalid room ID");
                return;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine($"Rejected: No nickname provided for {Context.ConnectionId}");
                await Clients.Caller.SendAsync("error", "Please provide nickname.");
                return;
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                Console.WriteLine($"Added {Context.ConnectionId} to group {roomId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add {Context.ConnectionId} to group {roomId}: {ex.Message}");
                await Clients.Caller.SendAsync("error", "Failed to join room.");
                return;
            }

            bool isNewGame = false;
            if (!Rooms.TryGetValue(roomId, out game))
            {
                game = new Game(roomId, Context.ConnectionId);
                Rooms[roomId] = game;
                isNewGame = true;
                Console.WriteLine($"New game {roomId} created with host {game.HostConnectionId}");
            }

            var added = game.AddPlayer(Context.ConnectionId, name);

            if (!added)
            {
                Console.WriteLine($"Rejected: Room full or started for {Context.ConnectionId}");
                await Clients.Caller.SendAsync("error", "Room is full or game has already started.");
                return;
            }

            await Clients.Group(roomId).SendAsync("player-joined", new { id = Context.ConnectionId, name, score = 0, precision = 0.0 });
            await Clients.Caller.SendAsync("players", game.Players.Values.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score, precision = p.Precision }));
            await Clients.Caller.SendAsync("new-host", game.HostConnectionId);
            Console.WriteLine($"Sent new-host to {Context.ConnectionId} with host ID {game.HostConnectionId}");

            if (isNewGame)
            {
                await Clients.Group(roomId).SendAsync("game-created", roomId);
            }
        }

        public async Task StartGame()
        {
            var connectionId = Context.ConnectionId;
            if (!ConnectionToRoomMap.TryGetValue(connectionId, out var roomId) || !Rooms.TryGetValue(roomId, out var game) || game == null)
            {
                await Clients.Caller.SendAsync("error", "Not in a game");
                return;
            }

            if (game.GameStatus != GameStatus.NotStarted)
            {
                await Clients.Caller.SendAsync("error", "The game has already started");
                return;
            }

            if (game.HostConnectionId != connectionId)
            {
                Console.WriteLine($"StartGame failed: Caller {connectionId} is not host {game.HostConnectionId}");
                await Clients.Caller.SendAsync("error", "You are not the host of the game. Only the host can start the game.");
                return;
            }

            foreach (var p in game.Players.Values)
            {
                p.Score = 0;
                p.Precision = 0.0;
            }

            await Clients.Group(roomId).SendAsync("players", game.Players.Values.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score, precision = p.Precision }));

            game.GameStatus = GameStatus.InProgress;

            var paragraph = await GenerateParagraphAsync();
            game.Paragraph = paragraph;
            game.ParagraphWords = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            await Clients.Group(roomId).SendAsync("game-started", paragraph);

            game.GameTimer = new CancellationTokenSource();
            _ = Task.Delay(60000, game.GameTimer.Token).ContinueWith(async t =>
            {
                if (!t.IsCanceled && Rooms.TryGetValue(roomId, out var g) && g.GameStatus == GameStatus.InProgress)
                {
                    g.GameStatus = GameStatus.Finished;
                    await Clients.Group(roomId).SendAsync("game-finished");
                    await Clients.Group(roomId).SendAsync("players", g.Players.Values.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score }));
                }
            });
        }

        public async Task PlayerTyped(string typed)
        {
            var connectionId = Context.ConnectionId;
            if (!ConnectionToRoomMap.TryGetValue(connectionId, out var roomId) || !Rooms.TryGetValue(roomId, out var game) || game == null)
            {
                await Clients.Caller.SendAsync("error", "Not in a game");
                return;
            }

            if (game.GameStatus != GameStatus.InProgress)
            {
                await Clients.Caller.SendAsync("error", "The game has not started yet");
                return;
            }

            var splitTyped = typed.TrimEnd().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int score = 0;

            for (int i = 0; i < splitTyped.Length && i < game.ParagraphWords.Length; i++)
            {
                if (splitTyped[i].Equals(game.ParagraphWords[i], StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }
                else
                {
                    break;
                }
            }

            double precision = 0.0;
            var typedText = typed.TrimEnd();
            var paragraphText = game.Paragraph.Substring(0, Math.Min(typedText.Length, game.Paragraph.Length));
            int correctChars = 0;
            for (int i = 0; i < Math.Min(typedText.Length, paragraphText.Length); i++)
            {
                if (char.ToLower(typedText[i]) == char.ToLower(paragraphText[i]))
                {
                    correctChars++;
                }
            }
            if (typedText.Length > 0)
            {
                precision = (double)correctChars / typedText.Length * 100;
            }

            bool isFinished = score == game.ParagraphWords.Length && typedText.Length >= game.Paragraph.Length;

            if (game.Players.TryGetValue(connectionId, out var player) && player != null)
            {
                player.Score = score;
                player.Precision = precision;
                Console.WriteLine($"Updated player {connectionId}: score={score}, precision={precision}");
                await Clients.Group(roomId).SendAsync("player-score", new { id = connectionId, score, precision });
            }

            if (isFinished)
            {
                game.GameStatus = GameStatus.Finished;
                if (game.GameTimer != null)
                {
                    game.GameTimer.Cancel();
                    game.GameTimer = null;
                }
                await Clients.Group(roomId).SendAsync("game-finished");
                await Clients.Group(roomId).SendAsync("players", game.Players.Values.Select(p => new { id = p.ConnectionId, name = p.Name, score = p.Score, precision = p.Precision }));
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionToRoomMap.TryRemove(Context.ConnectionId, out var roomId) &&
         Rooms.TryGetValue(roomId, out var game))
            {
                if (game.RemovePlayer(Context.ConnectionId, out var player) && player != null)
                {
                    await Clients.Group(roomId).SendAsync("player-left", player.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

                    if (game.IsEmpty)
                    {
                        Rooms.TryRemove(roomId, out _);
                        ConnectionToRoomMap.TryRemove(Context.ConnectionId, out _);
                        await Clients.Group(roomId).SendAsync("game-ended", roomId);
                    }
                    else if (game.HostConnectionId == Context.ConnectionId)
                    {
                        var newHost = game.Players.Values.FirstOrDefault();
                        if (newHost != null)
                        {
                            game.HostConnectionId = newHost.ConnectionId;
                            await Clients.Group(roomId).SendAsync("new-host", newHost.ConnectionId);
                        }
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        private async Task<string> GenerateParagraphAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("http://metaphorpsum.com/paragraphs/1/10");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    return data.Replace("\n", " ").Trim();
                }
            }
            catch { }

            return GenerateParagraphUsingLoremIpsum();
        }

        private string GenerateParagraphUsingLoremIpsum()
        {
            const string lorem = "Lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt mollit anim id est laborum";
            var words = lorem.Split(' ');
            var paragraph = new List<string>();
            var rnd = new Random();
            for (int i = 0; i < 50; i++)
            {
                paragraph.Add(words[rnd.Next(words.Length)]);
            }
            return string.Join(" ", paragraph).ToLower().Trim();
        }
    }
}


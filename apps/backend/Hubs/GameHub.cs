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
            ConnectionToRoomMap[Context.ConnectionId] = roomId;

            if (roomId.Length > 50 || name.Length > 50)
            {
                await Clients.Caller.SendAsync("Error", "Room ID or name too long.");
                return;
            }
            if (!Regex.IsMatch(roomId, @"^[a-zA-Z0-9_-]+$"))
            {
                await Clients.Caller.SendAsync("Error", "Invalid characters in room ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid room ID");
                return;
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                await Clients.Caller.SendAsync("Error", "Please provide nickname.");
                return;
            }

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", "Failed to join room.");
                return;
            }

            Game? game;
            bool isNewGame = false;
            if (!Rooms.TryGetValue(roomId, out game))
            {
                game = new Game(roomId, Context.ConnectionId);
                Rooms[roomId] = game;
                isNewGame = true;
            }

            var added = game.AddPlayer(Context.ConnectionId, name);

            if (!added)
            {
                await Clients.Caller.SendAsync("Error", "Room is full or game has already started.");
                return;
            }

            await Clients.Group(roomId).SendAsync("PlayerJoined", new { Id = Context.ConnectionId, Name = name, Score = 0 });
            await Clients.Caller.SendAsync("Players", game.Players.Values.Select(p => new { p.ConnectionId, p.Name, p.Score }));
            await Clients.Caller.SendAsync("NewHost", game.HostConnectionId);

            if (isNewGame)
            {
                await Clients.Group(roomId).SendAsync("GameCreated", roomId);
            }
        }

        public async Task StartGame()
        {
            var connectionId = Context.ConnectionId;
            if (!ConnectionToRoomMap.TryGetValue(connectionId, out var roomId) || !Rooms.TryGetValue(roomId, out var game) || game == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (game.GameStatus != GameStatus.NotStarted)
            {
                await Clients.Caller.SendAsync("Error", "The game has already started");
                return;
            }

            if (game.HostConnectionId != connectionId)
            {
                await Clients.Caller.SendAsync("Error", "You are not the host of the game. Only the host can start the game.");
                return;
            }

            foreach (var p in game.Players.Values)
            {
                p.Score = 0;
            }

            await Clients.Group(roomId).SendAsync("Players", game.Players.Values.Select(p => new { p.ConnectionId, p.Name, p.Score }));

            game.GameStatus = GameStatus.InProgress;

            var paragraph = await GenerateParagraphAsync();
            game.Paragraph = paragraph;
            game.ParagraphWords = paragraph.Split(' ');

            await Clients.Group(roomId).SendAsync("GameStarted", paragraph);

            game.GameTimer = new CancellationTokenSource();
            _ = Task.Delay(60000, game.GameTimer.Token).ContinueWith(async t =>
            {
                if (!t.IsCanceled && Rooms.TryGetValue(roomId, out var g) && g.GameStatus == GameStatus.InProgress)
                {
                    g.GameStatus = GameStatus.Finished;
                    await Clients.Group(roomId).SendAsync("GameFinished");
                    await Clients.Group(roomId).SendAsync("Players", g.Players.Values.Select(p => new { p.ConnectionId, p.Name, p.Score }));
                }
            });
        }

        public async Task PlayerTyped(string typed)
        {
            var connectionId = Context.ConnectionId;
            if (!ConnectionToRoomMap.TryGetValue(connectionId, out var roomId) || !Rooms.TryGetValue(roomId, out var game) || game == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (game.GameStatus != GameStatus.InProgress)
            {
                await Clients.Caller.SendAsync("Error", "The game has not started yet");
                return;
            }

            var splitTyped = typed.Split(' ');

            int score = 0;
            for (int i = 0; i < splitTyped.Length; i++)
            {
                if (i >= game.ParagraphWords.Length) break;
                if (splitTyped[i] == game.ParagraphWords[i])
                {
                    score++;
                }
                else
                {
                    break;
                }
            }

            if (game.Players.TryGetValue(connectionId, out var player) && player != null)
            {
                player.Score = score;
                await Clients.Group(roomId).SendAsync("PlayerScore", new { Id = connectionId, Score = score });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionToRoomMap.TryRemove(Context.ConnectionId, out var roomId) &&
         Rooms.TryGetValue(roomId, out var game))
            {
                if (game.RemovePlayer(Context.ConnectionId, out var player) && player != null)
                {
                    await Clients.Group(roomId).SendAsync("UserLeft", player);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

                    if (game.IsEmpty)
                    {
                        Rooms.TryRemove(roomId, out _);
                        ConnectionToRoomMap.TryRemove(Context.ConnectionId, out _);
                        await Clients.Group(roomId).SendAsync("GameEnded", roomId);
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
                    return data.Replace("\n", " ");
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
            return string.Join(" ", paragraph).ToLower();
        }
    }
}


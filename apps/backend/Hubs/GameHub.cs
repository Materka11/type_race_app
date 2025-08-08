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

            if (!Rooms.TryGetValue(roomId, out var game))
            {
                game = new Game(roomId);
                Rooms[roomId] = game;
                await Clients.Group(roomId).SendAsync("GameCreated", roomId);
            }

            var newPlayer = game.AddPlayer(Context.ConnectionId, name);

            if (!newPlayer)
            {
                await Clients.Caller.SendAsync("Error", "Room is full.");
                return;
            }

            await Clients.Group(roomId).SendAsync("UserJoined", name);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectionToRoomMap.TryRemove(Context.ConnectionId, out var roomId) &&
         Rooms.TryGetValue(roomId, out var game))
            {
                if (game.RemovePlayer(Context.ConnectionId, out var name) && name != null)
                {
                    await Clients.Group(roomId).SendAsync("UserLeft", name);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

                    if (game.IsEmpty)
                    {
                        Rooms.TryRemove(roomId, out _);
                        await Clients.Group(roomId).SendAsync("GameEnded", roomId);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

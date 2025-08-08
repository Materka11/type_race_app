using System.Collections.Concurrent;

namespace backend.Models
{
    public class Game
    {
        public string RoomId { get; }
        private readonly ConcurrentDictionary<string, string> Players = new();
        public bool IsEmpty => Players.Count == 0;
        public const int MaxPlayers = 10;

        public Game(string roomId)
        {
            RoomId = roomId;
        }

        public bool AddPlayer(string connectionId, string name)
        {
            if (Players.Count >= MaxPlayers)
            {
                return false;
            }
            Players[connectionId] = name;
            return true;
        }

        public bool RemovePlayer(string connectionId, out string? name)
        {
            if (Players.TryGetValue(connectionId, out name))
            {
                Players.TryRemove(connectionId, out name);
                return true;
            }
            name = null;
            return false;
        }
    }
}

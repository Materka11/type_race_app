using System.Collections.Concurrent;

namespace backend.Models
{
    public class Game
    {
        public string RoomId { get; }
        public string HostConnectionId { get; set; }
        public GameStatus GameStatus { get; set; } = GameStatus.NotStarted;
        public string Paragraph { get; set; } = "";
        public string[] ParagraphWords { get; set; } = Array.Empty<string>();
        public ConcurrentDictionary<string, Player> Players { get; } = new();
        public bool IsEmpty => Players.IsEmpty;
        public const int MaxPlayers = 10;
        public CancellationTokenSource? GameTimer { get; set; }

        public Game(string roomId, string hostConnectionId)
        {
            RoomId = roomId;
            HostConnectionId = hostConnectionId;
            Console.WriteLine($"Game created for room {roomId} with host {hostConnectionId}");
        }

        public bool AddPlayer(string connectionId, string name)
        {
            if (Players.Count >= MaxPlayers)
            {
                return false;
            }
            if (GameStatus != GameStatus.NotStarted)
            {
                return false;
            }
            return Players.TryAdd(connectionId, new Player { ConnectionId = connectionId, Name = name, Score = 0, Precision = 0.0 });
        }

        public bool RemovePlayer(string connectionId, out Player? player)
        {
            return Players.TryRemove(connectionId, out player);
        }
    }
}

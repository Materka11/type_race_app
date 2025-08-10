namespace backend.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Score { get; set; } = 0;
        public double Precision { get; set; }
    }
}

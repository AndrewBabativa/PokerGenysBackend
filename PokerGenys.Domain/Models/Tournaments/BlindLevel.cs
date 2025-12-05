namespace PokerGenys.Domain.Models.Tournaments
{
    public class BlindLevel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int LevelNumber { get; set; }
        public int DurationSeconds { get; set; }
        public int SmallBlind { get; set; }
        public int BigBlind { get; set; }
        public int Ante { get; set; }
        public bool IsBreak { get; set; }
        public bool AllowRebuy { get; set; }
        public bool AllowAddon { get; set; }
    }
}

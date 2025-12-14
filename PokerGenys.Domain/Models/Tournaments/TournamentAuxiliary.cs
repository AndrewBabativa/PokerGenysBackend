using PokerGenys.Domain.Enums;


namespace PokerGenys.Domain.Models.Tournaments
{
    public class RegistrationResult
    {
        public TournamentRegistration Registration { get; set; }
        public string? InstructionType { get; set; }
        public string? SystemMessage { get; set; }
        public TournamentStatsDto? NewStats { get; set; }
    }

    public class RemoveResult
    {
        public bool Success { get; set; }
        public string? InstructionType { get; set; }
        public string? Message { get; set; }
        public string? FromTable { get; set; }
        public string? ToTable { get; set; }
        public object? Data { get; set; }
    }

    public class TournamentStatsDto
    {
        public int Entries { get; set; }
        public int Active { get; set; }
        public decimal PrizePool { get; set; }
    }

    public class TournamentState
    {
        public int CurrentLevel { get; set; }
        public int TimeRemaining { get; set; }
        public TournamentStatus Status { get; set; } 
        public int RegisteredCount { get; set; }
        public decimal PrizePool { get; set; }
        public string Blinds { get; set; }
        public bool IsFinalTable { get; set; }
    }
}
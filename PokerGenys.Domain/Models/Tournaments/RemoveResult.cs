namespace PokerGenys.Domain.Models.Tournaments
{
    public class RemoveResult
    {
        public bool Success { get; set; }
        public string? InstructionType { get; set; } 
        public string? Message { get; set; }
        public string? FromTable { get; set; }
        public string? ToTable { get; set; }
    }
}
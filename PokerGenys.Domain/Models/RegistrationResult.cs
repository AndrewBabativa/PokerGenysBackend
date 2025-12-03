namespace PokerGenys.Domain.Models
{
    public class RegistrationResult
    {
        public TournamentRegistration Registration { get; set; }
        public string? InstructionType { get; set; } // "INFO_ALERT"
        public string? SystemMessage { get; set; }   // El mensaje para el Admin/TV
    }
}

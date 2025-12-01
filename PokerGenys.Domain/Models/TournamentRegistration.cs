using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models
{
    public class TournamentRegistration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TournamentId { get; set; }

        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string? MemberId { get; set; }

        public string? SeatId { get; set; }
        public string? TableId { get; set; }

        public int Chips { get; set; }

        public string RegistrationType { get; set; } = "Standard";

        public decimal PaidAmount { get; set; }
        public string? PaymentMethod { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Active";
        public DateTime? EliminatedAt { get; set; }

        public decimal? PayoutAmount { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }
    }
}

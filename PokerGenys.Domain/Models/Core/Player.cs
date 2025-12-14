using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Core
{
    public class Player
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Nickname { get; set; }
        public string? DocumentId { get; set; }
        public string? PhotoUrl { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? BirthDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public PlayerStatus Status { get; set; } = PlayerStatus.Active;

        [BsonRepresentation(BsonType.String)]
        public PlayerType Type { get; set; } = PlayerType.Standard;

        public string? InternalNotes { get; set; }

        public PlayerFinancials Financials { get; set; } = new PlayerFinancials();
        public PlayerStats Stats { get; set; } = new PlayerStats();

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [BsonIgnore]
        public string DisplayName => !string.IsNullOrEmpty(Nickname) ? Nickname : $"{FirstName} {LastName}".Trim();
    }

    public class PlayerFinancials
    {
        public decimal CreditBalance { get; set; } = 0;
        public decimal TotalDebt { get; set; } = 0;
        public decimal TotalRakeGenerated { get; set; } = 0;
    }

    public class PlayerStats
    {
        public int TotalSessionsPlayed { get; set; } = 0;
        public int TotalTournamentsPlayed { get; set; } = 0;
        public int TournamentsWon { get; set; } = 0;
        public DateTime? LastVisit { get; set; }
        public int LoyaltyPoints { get; set; } = 0;
    }
}
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

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

    public class PayoutTier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Rank { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Percentage { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal FixedAmount { get; set; }
    }

    public class ClockState
    {
        public bool IsPaused { get; set; } = true;
        public double SecondsRemaining { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class TournamentTable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TournamentId { get; set; }
        public int TableNumber { get; set; }
        public string Name { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public TournamentTableStatus Status { get; set; } = TournamentTableStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public Guid? CurrentDealerId { get; set; }
        public int MaxSeats { get; set; } = 9;
        public int ActivePlayersCount { get; set; } = 0;
    }

    // CONFIGURACIONES
    public class SeatingConfiguration
    {
        public int Tables { get; set; } = 1;
        public int SeatsPerTable { get; set; } = 9;
        public int FinalTableSize { get; set; } = 9;
        [BsonRepresentation(BsonType.String)]
        public SeatingMode InitialSeatingMode { get; set; } = SeatingMode.Random;
        [BsonRepresentation(BsonType.String)]
        public TableBalancingMode TableBalancingMode { get; set; } = TableBalancingMode.Auto;
    }

    public class RebuyConfig
    {
        public bool Enabled { get; set; } = false;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal RebuyCost { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal RebuyHouseFee { get; set; }
        public int RebuyChips { get; set; }
        public bool AutoRebuy { get; set; }
        public int MaxRebuysPerPlayer { get; set; }
        public int UntilLevel { get; set; }
    }

    public class AddonConfig
    {
        public bool Enabled { get; set; } = false;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AddonCost { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AddonHouseFee { get; set; }
        public int AddonChips { get; set; }
        public int MaxAddonsPerPlayer { get; set; }
        public int AllowedLevel { get; set; }
    }

    public class BountyConfig
    {
        public bool Enabled { get; set; } = false;
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BountyCost { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PrizePerPlayer { get; set; }
        [BsonRepresentation(BsonType.String)]
        public BountyType Type { get; set; } = BountyType.Standard;
        public decimal ProgressivePercentage { get; set; }
    }
}
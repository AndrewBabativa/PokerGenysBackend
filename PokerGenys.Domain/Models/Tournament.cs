// Domain/Models/Tournament.cs
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models
{
    public class Tournament
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = "Scheduled";

        public decimal BuyIn { get; set; }
        public decimal Fee { get; set; }
        public decimal Guaranteed { get; set; }
        public int StartingChips { get; set; }

        public SeatingConfiguration Seating { get; set; } = new();
        public RebuyConfig RebuyConfig { get; set; } = new();
        public AddonConfig AddonConfig { get; set; } = new();
        public BountyConfig BountyConfig { get; set; } = new();

        public List<BlindLevel> Levels { get; set; } = new();
        public List<PayoutTier> Payouts { get; set; } = new();
        public List<TournamentRegistration> Registrations { get; set; } = new();

        // 🔹 Campos nuevos
        public DateTime? StartTime { get; set; }   // Hora exacta de inicio
        public int CurrentLevel { get; set; } = 1; // Nivel actual
        public decimal PrizePool { get; set; } = 0;
    }


    public class TournamentState
    {
        public int CurrentLevel { get; set; }
        public int TimeRemaining { get; set; } // en segundos
        public string Status { get; set; } = "Scheduled";
        public int RegisteredCount { get; set; }
        public decimal PrizePool { get; set; }
    }

    public class SeatingConfiguration
    {
        public int Tables { get; set; } = 1;
        public int SeatsPerTable { get; set; } = 2;
        public int FinalTableSize { get; set; } = 2;
        public string InitialSeatingMode { get; set; } = "Standard";
        public string TableBalancingMode { get; set; } = "None";
    }

    public class RebuyConfig
    {
        public bool Enabled { get; set; } = false;
        public decimal RebuyCost { get; set; }
        public decimal RebuyHouseFee { get; set; }
        public int RebuyChips { get; set; }
        public bool AutoRebuy { get; set; }
        public int MaxRebuysPerPlayer { get; set; }
        public int UntilLevel { get; set; }
    }

    public class AddonConfig
    {
        public bool Enabled { get; set; } = false;
        public decimal AddonCost { get; set; }
        public decimal AddonHouseFee { get; set; }
        public int AddonChips { get; set; }
        public int MaxAddonsPerPlayer { get; set; }
        public int AllowedLevel { get; set; }
    }

    public class BountyConfig
    {
        public bool Enabled { get; set; } = false;
        public decimal BountyCost { get; set; }
        public decimal PrizePerPlayer { get; set; }
        public string Type { get; set; } = "normal";
        public decimal ProgressivePercentage { get; set; }
    }

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
        public decimal Percentage { get; set; }
        public decimal FixedAmount { get; set; }
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
    // --- NUEVA CLASE PARA EL ESTADO DEL RELOJ ---
    public class TournamentClockState
    {
        public bool IsPaused { get; set; } = true;
        public double SecondsRemaining { get; set; } // Tiempo guardado al pausar
        public DateTime? LastUpdatedAt { get; set; } // Momento de la última pausa/inicio
    }

    [BsonIgnoreExtraElements]
    public class Tournament
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkingDayId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TournamentStatus Status { get; set; } = TournamentStatus.Scheduled;

        // --- FINANCIALS ---
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BuyIn { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Fee { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Guaranteed { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PrizePool { get; set; } = 0;
        public int StartingChips { get; set; }

        // --- CONFIGURATIONS ---
        public SeatingConfiguration Seating { get; set; } = new();
        public RebuyConfig RebuyConfig { get; set; } = new();
        public AddonConfig AddonConfig { get; set; } = new();
        public BountyConfig BountyConfig { get; set; } = new();

        public List<BlindLevel> Levels { get; set; } = new();
        public List<PayoutTier> Payouts { get; set; } = new();

        // --- REAL-TIME STATE (OPTIMIZADO) ---
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int CurrentLevel { get; set; } = 1;

        // Propiedad nueva para manejar la Pausa correctamente
        public TournamentClockState ClockState { get; set; } = new();

        public int TotalEntries { get; set; }
        public int ActivePlayers { get; set; }

        public List<TournamentTable> Tables { get; set; } = new();
        public List<TournamentRegistration> Registrations { get; set; } = new();
        public List<TournamentTransaction> Transactions { get; set; } = new();
    }
}
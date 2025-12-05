using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace PokerGenys.Domain.Models.Tournaments
{
    [BsonIgnoreExtraElements]
    public class Tournament
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        // VITAL FOR REPORTS: Link to the specific working day (Jornada)
        public Guid WorkingDayId { get; set; }

        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        [BsonRepresentation(BsonType.String)]
        public TournamentStatus Status { get; set; } = TournamentStatus.Scheduled;

        // --- FINANCIALS (Decimal128 is MANDATORY for monetary precision in Mongo) ---
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BuyIn { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Fee { get; set; } // House Rake

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Guaranteed { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PrizePool { get; set; } = 0;

        public int StartingChips { get; set; }

        // --- CONFIGURATIONS (Value Objects) ---
        public SeatingConfiguration Seating { get; set; } = new();
        public RebuyConfig RebuyConfig { get; set; } = new();
        public AddonConfig AddonConfig { get; set; } = new();
        public BountyConfig BountyConfig { get; set; } = new();

        public List<BlindLevel> Levels { get; set; } = new();
        public List<PayoutTier> Payouts { get; set; } = new();

        // --- REAL-TIME STATE ---
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; } // To calculate duration
        public int CurrentLevel { get; set; } = 1;

        // COUNTERS (Snapshot to avoid counting the registration list every time)
        public int TotalEntries { get; set; }
        public int ActivePlayers { get; set; }


        [BsonIgnore]
        public List<TournamentTable> Tables { get; set; } = new();

        [BsonIgnore]
        public List<TournamentRegistration> Registrations { get; set; } = new();

        [BsonIgnore]
        public List<TournamentTransaction> Transactions { get; set; } = new();
    }
}
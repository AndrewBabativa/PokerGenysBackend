using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Models.CashGame;

namespace PokerGenys.Domain.Models
{
    public class TableInstance
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DayId { get; set; }

        public string Name { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public TableStatus Status { get; set; } = TableStatus.Open;

        public int MaxPlayers { get; set; } = 9;
        public decimal InitialBuyInBase { get; set; }

        public Guid? CurrentDealerId { get; set; }

        // --- NUEVOS CAMPOS ---

        // Usamos decimal para dinero (mejor precisión que double)
        // Nullable (?) porque al crear la mesa esto aún no existe
        public decimal? TotalRake { get; set; }

        public string? CloseNotes { get; set; }

        // ---------------------

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
}
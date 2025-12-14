using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Enums;

namespace PokerGenys.Domain.Models.CashGame
{
    public class CashTable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkingDayId { get; set; }
        public string Name { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public TableStatus Status { get; set; } = TableStatus.Open;

        public int MaxPlayers { get; set; } = 9;
        public decimal InitialBuyInBase { get; set; }
        public Guid? CurrentDealerId { get; set; }

        public decimal? TotalRake { get; set; }
        public string? CloseNotes { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
}
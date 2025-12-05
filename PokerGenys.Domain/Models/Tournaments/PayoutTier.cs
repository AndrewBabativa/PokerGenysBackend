using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class PayoutTier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int Rank { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Percentage { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal FixedAmount { get; set; }
    }
}

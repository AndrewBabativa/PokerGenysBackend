using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
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

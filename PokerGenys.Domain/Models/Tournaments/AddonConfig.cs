using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
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
}

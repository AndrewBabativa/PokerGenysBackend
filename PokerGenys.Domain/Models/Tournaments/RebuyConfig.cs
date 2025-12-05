using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
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
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PokerGenys.Domain.Models.Tournaments
{
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
}

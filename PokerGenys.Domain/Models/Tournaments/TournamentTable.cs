using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Models.Tournaments;

namespace PokerGenys.Domain.Models
{
    public class TournamentTable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TournamentId { get; set; }

        public int TableNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public TournamentTableStatus Status { get; set; } = TournamentTableStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public Guid? CurrentDealerId { get; set; }

        public int MaxSeats { get; set; } = 9;

        public int ActivePlayersCount { get; set; } = 0;
    }
}
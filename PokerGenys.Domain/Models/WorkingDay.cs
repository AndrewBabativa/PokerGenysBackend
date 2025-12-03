using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models
{
    public class WorkingDay
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        [BsonRepresentation(BsonType.String)]
        public WorkingDayStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public string? Notes { get; set; }
    }
}
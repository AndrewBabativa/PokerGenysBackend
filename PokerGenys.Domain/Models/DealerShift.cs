using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models
{
    public class DealerShift
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DayId { get; set; }
        public Guid TableId { get; set; }
        public Guid DealerId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
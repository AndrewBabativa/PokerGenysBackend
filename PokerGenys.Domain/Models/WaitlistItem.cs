using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models
{
    public class WaitlistItem
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TableId { get; set; }
        public Guid PlayerId { get; set; }

        // Guardamos el nombre aquí para evitar un JOIN (lookup) extra
        public string PlayerName { get; set; } = string.Empty;

        public int Priority { get; set; } // 1 = Más alta
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
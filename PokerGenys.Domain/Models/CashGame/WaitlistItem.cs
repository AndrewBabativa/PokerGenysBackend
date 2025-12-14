using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models.CashGame
{
    public class WaitlistItem
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Referencia a CashTable
        public Guid TableId { get; set; }

        // Referencia a Player (Core)
        public Guid PlayerId { get; set; }

        // Guardamos el nombre aquí para evitar un JOIN (lookup) extra en la UI rápida
        public string PlayerName { get; set; } = string.Empty;

        public int Priority { get; set; } // 1 = Más alta (el primero en la lista)
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
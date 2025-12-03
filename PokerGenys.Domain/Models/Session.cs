using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Transactions;

namespace PokerGenys.Domain.Models
{
    public class Session
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DayId { get; set; }
        public Guid TableId { get; set; }
        public Guid PlayerId { get; set; }

        public decimal Stack { get; set; } // Stack actual visual

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public decimal InitialBuyIn { get; set; }
        public decimal TotalInvestment { get; set; }
        public decimal CashOut { get; set; }

        // Las transacciones viven dentro de la sesión (Document Embedding)
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
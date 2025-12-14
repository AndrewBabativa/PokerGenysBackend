using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PokerGenys.Domain.Models.Core;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.CashGame
{
    public class CashSession
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkingDayId { get; set; }
        public Guid TableId { get; set; }
        public Guid PlayerId { get; set; }

        public decimal Stack { get; set; } // Stack actual visual

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        public decimal InitialBuyIn { get; set; }
        public decimal TotalInvestment { get; set; }
        public decimal CashOut { get; set; }

        // Transacciones embebidas
        public List<FinancialTransaction> Transactions { get; set; } = new List<FinancialTransaction>();

        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
using System.Collections.Generic;

namespace PokerGenys.Domain.DTOs.Audit
{
    public class CashAuditResult
    {
        public decimal TotalBuyIns { get; set; }
        public decimal TotalCashOuts { get; set; }
        public decimal TotalRakeGenerated { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    }

    public class TournamentAuditResult
    {
        public decimal TotalCollected { get; set; }
        public decimal TotalPayouts { get; set; }
        public decimal TotalFeesGenerated { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
    }
}
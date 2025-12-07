using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Reports
{
    public class TreasurySummaryDto
    {
        public decimal TotalCashReceived { get; set; }
        public decimal TotalTransfersReceived { get; set; }
        public Dictionary<string, decimal> BankBreakdown { get; set; } = new()
        {
            { "Bancolombia", 0 }, { "Nequi", 0 }, { "Daviplata", 0 }, { "Other", 0 }
        };
        public decimal TotalCourtesies { get; set; }
        public decimal TotalInternalBalanceUsed { get; set; }
        public decimal TotalPendingDebt { get; set; }
    }
}

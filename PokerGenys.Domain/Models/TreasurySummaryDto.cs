using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models
{
    public class TreasurySummaryDto
    {
        // Dinero REAL en Caja Física
        public decimal TotalCashReceived { get; set; }

        // Dinero VIRTUAL (Bancos)
        public decimal TotalTransfersReceived { get; set; }
        public Dictionary<string, decimal> BankBreakdown { get; set; } = new Dictionary<string, decimal>();

        // Dinero que NO entra (Marketing/Regalos)
        public decimal TotalCourtesies { get; set; }

        // Dinero INTERNO (Saldo a favor usado)
        public decimal TotalInternalBalanceUsed { get; set; }

        // Dinero PENDIENTE (Deudores Morosos)
        public decimal TotalPendingDebt { get; set; }
    }
}

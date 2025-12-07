using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Reports
{
    public class CashGameDailySummaryDto
    {
        public int TotalTables { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalBuyIns { get; set; }
        public decimal TotalCashOuts { get; set; }
        public decimal GrossProfit { get; set; } // (BuyIns - CashOuts)
    }
}

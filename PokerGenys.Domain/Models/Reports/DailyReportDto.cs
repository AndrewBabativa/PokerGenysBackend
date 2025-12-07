using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Reports
{
    public class DailyReportDto
    {
        public DateTime Date { get; set; }

        // 1. Tesorería UNIFICADA (La verdad de la plata)
        public TreasurySummaryDto GlobalTreasury { get; set; } = new();

        // Ganancia Neta Total (Rake Cash + Rake Torneos + Ventas Rest)
        public decimal TotalNetProfit { get; set; }

        // 2. Desgloses
        public CashGameDailySummaryDto CashGames { get; set; } = new();
        public TournamentDailySummaryDto Tournaments { get; set; } = new();
        public decimal RestaurantTotalSales { get; set; }
    }
}

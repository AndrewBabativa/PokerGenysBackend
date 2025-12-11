using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models
{
    public class PlayerReportDto
    {
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = "Jugador";
        public string Duration { get; set; } // Formato "5h 30m"
        public decimal BuyIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal Restaurant { get; set; }
        public decimal NetResult { get; set; } // (CashOut - BuyIn - Restaurant)
        public bool HasPendingDebt { get; set; }
        public int totalMinutes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Reports
{
    public class TournamentDailySummaryDto
    {
        public int TotalTournaments { get; set; }
        public int TotalEntries { get; set; }
        public int TotalAddons { get; set; }

        // Finanzas
        public decimal TotalCollected { get; set; } // Dinero bruto entrado
        public decimal TotalPrizePool { get; set; }
        public decimal TotalStaffFee { get; set; }
        public decimal TotalRake { get; set; } // Ganancia Neta

        public List<TournamentEventDto> Events { get; set; } = new();
    }
}

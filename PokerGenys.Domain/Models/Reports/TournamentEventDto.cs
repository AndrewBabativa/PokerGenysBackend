using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Reports
{
    public class TournamentEventDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Entries { get; set; }
        public decimal Guaranteed { get; set; }
        public decimal PrizePool { get; set; }
        public decimal Overlay { get; set; } // Garantizado - Recaudado (si es positivo, la casa pierde)
        public string Status { get; set; }
    }
}

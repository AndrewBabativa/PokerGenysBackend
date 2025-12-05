using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class TournamentStatsDto
    {
        public int Entries { get; set; }
        public int Active { get; set; }
        public decimal PrizePool { get; set; }
    }
}

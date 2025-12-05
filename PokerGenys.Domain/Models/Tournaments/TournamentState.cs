using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class TournamentState
    {
        public int CurrentLevel { get; set; }
        public int TimeRemaining { get; set; } 
        public TournamentStatus Status { get; set; } 
        public int RegisteredCount { get; set; }
        public decimal PrizePool { get; set; }
    }
}

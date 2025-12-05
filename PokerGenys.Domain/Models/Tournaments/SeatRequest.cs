using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class SeatRequest
    {
        public string TableId { get; set; } = "";
        public string SeatId { get; set; } = "";
    }
}

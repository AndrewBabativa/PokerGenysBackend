using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class PayoutRequest
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Cash";
        public string? Notes { get; set; }
    }
}

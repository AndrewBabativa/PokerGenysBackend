using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class RegisterRequest
    {
        public string PlayerName { get; set; } = "";
        public string PaymentMethod { get; set; } = "Cash";
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }
}

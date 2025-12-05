using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerGenys.Domain.Models.Tournaments
{
    public class ServiceSaleRequest
    {
        public Guid? PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Venta";
        public Dictionary<string, object> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = "Cash";
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }
}

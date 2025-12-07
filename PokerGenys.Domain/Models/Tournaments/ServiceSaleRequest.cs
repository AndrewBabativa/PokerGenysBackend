namespace PokerGenys.Domain.Models.Tournaments
{
    public class ServiceSaleRequest
    {
        public Guid? PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Venta";
        public Dictionary<string, string> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = "Cash";
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }
}

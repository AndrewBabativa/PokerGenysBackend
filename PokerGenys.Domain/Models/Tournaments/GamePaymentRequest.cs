namespace PokerGenys.Domain.Models.Tournaments
{
    public class GamePaymentRequest
    {
        public string PaymentMethod { get; set; } = "Cash";
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }
}

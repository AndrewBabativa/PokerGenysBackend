namespace PokerGenys.Domain.Models
{
    public enum TournamentTransactionType
    {
        BuyIn,
        Rebuy,
        AddOn,
        Payout,      // Salida (Premio)
        StaffTip,    // Salida (Propina Staff)
        HouseRake,   // Ingreso (Comisión Casa - Calculado o Retirado)
        Expense      // Salida (Gasto operativo: comida, etc)
    }

    public class TournamentTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TournamentId { get; set; }
        public Guid? PlayerId { get; set; } // Null si es un gasto general (ej: pago a dealer)

        public TournamentTransactionType Type { get; set; }
        public decimal Amount { get; set; } // Positivo = Entrada, Negativo = Salida
        public string Method { get; set; } = "Cash"; // "Cash", "Transfer", "CreditCard"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } // "Rebuy Nivel 3", "Pago Premio 2do Lugar"

        public string CreatedBy { get; set; } // Usuario que registró el movimiento (Auditoría)
    }
}
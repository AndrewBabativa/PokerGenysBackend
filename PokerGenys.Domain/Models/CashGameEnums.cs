namespace PokerGenys.Domain.Models.CashGame
{
    // =========================================================
    // ENUMS DEL SISTEMA DE MESAS CASH
    // =========================================================

    /// <summary>
    /// Estado de la jornada laboral general del casino/club.
    /// </summary>
    public enum WorkingDayStatus
    {
        Open,
        Closed
    }

    /// <summary>
    /// Estado individual de una mesa.
    /// </summary>
    public enum TableStatus
    {
        Open,
        Closed
    }

    /// <summary>
    /// Tipos de movimientos financieros registrados en las sesiones.
    /// </summary>
    public enum TransactionType
    {
        BuyIn,       // Compra inicial o recarga de fichas
        ReBuy,       // Recompra de fichas
        Sale,        // Venta (ej. comida, bebida, cigarrillos) cargada a la cuenta
        CashOut,     // Retiro de fichas al finalizar sesión
        Jackpot,     // Pago de premio Jackpot
        DebtPayment  // Pago de deuda interna (técnico)
    }

    /// <summary>
    /// Categoría secundaria para transacciones de tipo 'Sale'.
    /// </summary>
    public enum TransactionCategory
    {
        Clear,
        Restaurant,
        Other
    }

    /// <summary>
    /// Método utilizado para pagar un BuyIn o cancelar una deuda.
    /// </summary>
    public enum PaymentMethod
    {
        Cash,       // Efectivo
        Transfer,   // Transferencia bancaria
        Courtesy,   // Cortesía de la casa
        Saldofavor  // Uso de saldo a favor acumulado
    }

    /// <summary>
    /// Estado financiero de una transacción.
    /// </summary>
    public enum PaymentStatus
    {
        Paid,    // Pagado al momento
        Pending  // Queda debiendo (Fia)
    }

    /// <summary>
    /// Entidad financiera si el método de pago es Transferencia.
    /// </summary>
    public enum BankMethod
    {
        Bancolombia,
        Nequi,
        Daviplata,
        Other
    }


}
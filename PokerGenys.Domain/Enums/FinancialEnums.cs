using System.Text.Json.Serialization;

namespace PokerGenys.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        Cash,       // Efectivo físico
        Transfer,   // Bancos digitales
        CreditCard, // Datáfono
        Balance,    // Saldo interno del jugador
        Courtesy,   // Regalo de la casa
        Mixed       // Parte efectivo / Parte transferencia
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentProvider
    {
        None,
        Bancolombia,
        Nequi,
        Daviplata,
        BBVA,
        Davivienda,
        Zelle,
        USDT,
        Bold
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus { Pending, Paid, Refunded, Voided }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionType
    {
        // Ingresos
        BuyIn,          // Cash & Torneo
        ReBuy,          // Cash & Torneo
        AddOn,          // Torneo
        LateRegistration, // Torneo
        ServiceSale,    // Restaurante/Tienda

        // Salidas
        CashOut,        // Cash
        Payout,         // Torneo (Premio)
        Expense,        // Gasto operativo

        // Interno
        Rake,           // Ganancia explícita
        JackpotContribution,
        DebtPayment,    // Pago de deuda
        Penalty,
        Bounty          // Recompensa por eliminación
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionSource
    {
        CashGame,
        Tournament,
        Restaurant,
        Admin
    }
}
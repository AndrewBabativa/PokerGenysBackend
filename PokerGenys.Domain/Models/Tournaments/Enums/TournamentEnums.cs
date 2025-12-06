using System.Text.Json.Serialization;

namespace PokerGenys.Domain.Models.Tournaments
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentStatus
    {
        Scheduled,
        LateRegistration, // Registro tardío abierto
        Running,          // Torneo en curso, registro cerrado
        Paused,
        Finished,
        Canceled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationStatus
    {
        Active,
        Eliminated,
        Unregistered // Por si se devuelve el buy-in
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationType
    {
        Standard,
        SatelliteWinner,
        Invitation,
        Reentry
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WorkingDayStatus
    {
        Open,
        Closing,
        Closed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BountyType
    {
        Standard,      // Bounty fijo por eliminación
        Progressive,   // PKO (Progressive Knockout)
        Mystery        // Sobres sorpresa
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeatingMode
    {
        Random,        // Asignación aleatoria automática (Lo estándar)
        Manual,        // El director asigna a dedo
        Family         // Evita que jugadores específicos se sienten juntos (opcional)
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TableBalancingMode
    {
        None,          // No balancear (peligroso)
        Auto,          // El sistema sugiere a quién mover
        Strict         // El sistema fuerza el movimiento (regla de diferencia de 1 o 2)
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentTableStatus
    {
        Active,        // Mesa en juego
        Broken,        // Mesa "rompida" (cerrada para balancear)
        FinalTable,    // Mesa final (cambia la dinámica/luces)
        Paused,         // Juego detenido solo en esta mesa
        Finished
    }

    public enum TransactionType
    {
        BuyIn,              // Entrada inicial
        ReBuy,              // Recompra
        AddOn,              // Add-on al final del registro
        LateRegistration,   // Entrada tardía con costo distinto
        Penalty,            // Penalización (deducción de saldo por sanción)
        PrizePayout,        // Pago de premio estándar por posiciones
        Bounty,             // Pago o cobro de recompensa (si el torneo es bounty)
        HouseRake,
        Payout,
        Expense,
        ServiceSale
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        Cash,           // Efectivo en caja física
        Transfer,       // Transferencia digital (requiere especificar Banco)
        CreditCard,     // Datáfono
        Balance,        // Saldo a favor / Crédito interno del jugador
        Courtesy,       // 100% Gratis (Bonificación de la casa)
        Mixed           // Parte efectivo / Parte transferencia (Complejo, pero real)
    }

    // 2. PROVEEDOR ESPECÍFICO (¿A qué cuenta entró?)
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentProvider
    {
        None,           // Para efectivo o cortesías

        // Bancos Locales (Ajusta según tu país)
        Bancolombia,
        Nequi,
        Daviplata,
        BBVA,
        Davivienda,

        // Internacional / Crypto
        Zelle,
        PayPal,
        USDT,           // Tether (Crypto)
        Bitcoin,

        // Otros
        Bold,           // Datáfono
        Redeban
    }

}
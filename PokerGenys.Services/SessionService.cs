using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;

        public SessionService(ISessionRepository repo) => _repo = repo;

        public Task<List<Session>> GetAllAsync() => _repo.GetAllActiveAsync();

        public Task<List<Session>> GetAllByTableIdAsync(Guid tableId) => _repo.GetAllByTableIdAsync(tableId);

        public async Task<Session> CreateAsync(Session session)
        {
            if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();

            // LOGICA CRÍTICA: Crear transacción inicial de BuyIn automáticamente
            if (session.InitialBuyIn > 0)
            {
                // Aseguramos que la lista no sea null
                if (session.Transactions == null) session.Transactions = new List<Transaction>();

                var hasBuyIn = session.Transactions.Any(t => t.Type == TransactionType.BuyIn);

                if (!hasBuyIn)
                {
                    // FIX: Definir valores por defecto para evitar el crash si la lista está vacía
                    var defaultStatus = PaymentStatus.Pending; // Por defecto asumimos pendiente si no se especifica
                    var defaultMethod = PaymentMethod.Cash;    // Por defecto efectivo

                    if (session.Transactions.Count > 0)
                    {
                        defaultStatus = session.Transactions[0].PaymentStatus ?? PaymentStatus.Pending;
                        defaultMethod = session.Transactions[0].PaymentMethod ?? PaymentMethod.Cash;
                    }

                    session.Transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        Type = TransactionType.BuyIn,
                        Amount = session.InitialBuyIn,
                        // Usamos las variables seguras calculadas arriba
                        PaymentStatus = defaultStatus,
                        PaymentMethod = defaultMethod,
                        CreatedAt = DateTime.UtcNow,
                        Description = "Initial BuyIn (Auto-generated)"
                    });
                }
            }

            session.TotalInvestment = session.InitialBuyIn;
            session.StartTime = DateTime.UtcNow;

            return await _repo.CreateAsync(session);
        }

        public async Task<Session?> UpdateAsync(Session session)
        {
            var existing = await _repo.GetByIdAsync(session.Id);
            if (existing == null) return null;
            await _repo.UpdateAsync(session);
            return session;
        }

        public async Task<TableReportDto> GetTableReportAsync(Guid tableId)
        {
            // 1. TRAER TODO (Historial completo de la mesa)
            var sessions = await _repo.GetByTableIdAsync(tableId);

            var report = new TableReportDto();

            // Inicializamos contadores bancarios para que aparezcan en 0 si no hay movs
            report.Treasury.BankBreakdown = new Dictionary<string, decimal>
            {
                { "Bancolombia", 0 }, { "Nequi", 0 }, { "Daviplata", 0 }, { "Other", 0 }
            };

            foreach (var session in sessions)
            {
                // A. Variables locales por jugador
                decimal pBuyIn = 0;
                decimal pRest = 0;
                decimal pCashOut = session.CashOut; // Valor base final
                bool pHasDebt = false;

                // B. Calcular Tiempo Jugado
                var endTime = session.EndTime ?? DateTime.UtcNow;
                var timeSpan = endTime - session.StartTime;
                string formattedDuration = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

                if (session.Transactions != null)
                {
                    foreach (var tx in session.Transactions)
                    {
                        // ==========================================
                        // FASE 1: VOLUMEN OPERATIVO (¿Qué pasó en la mesa?)
                        // ==========================================
                        switch (tx.Type)
                        {
                            case TransactionType.BuyIn:
                            case TransactionType.ReBuy:
                                report.TotalBuyIns += tx.Amount;
                                pBuyIn += tx.Amount;
                                break;

                            case TransactionType.Sale:
                                report.TotalRestaurantSales += tx.Amount;
                                pRest += tx.Amount;
                                break;

                            case TransactionType.CashOut:
                                report.TotalCashOuts += tx.Amount;
                                break;
                        }

                        // ==========================================
                        // FASE 2: TESORERÍA (¿Dónde está la plata?)
                        // ==========================================

                        // CASO A: DEUDA PENDIENTE (Fiado)
                        if (tx.PaymentStatus == PaymentStatus.Pending)
                        {
                            report.Treasury.TotalPendingDebt += tx.Amount;
                            pHasDebt = true;
                        }
                        // CASO B: PAGADO (Entró dinero o cortesía)
                        else if (tx.PaymentStatus == PaymentStatus.Paid)
                        {
                            // IMPORTANTE: Contamos transacciones normales Y pagos de deuda
                            // (Un DebtPayment trae dinero fresco a la caja)
                            if (tx.Type != TransactionType.CashOut) // CashOut es salida, no entrada
                            {
                                switch (tx.PaymentMethod)
                                {
                                    case PaymentMethod.Cash:
                                        report.Treasury.TotalCashReceived += tx.Amount;
                                        break;

                                    case PaymentMethod.Courtesy:
                                        report.Treasury.TotalCourtesies += tx.Amount;
                                        break;

                                    case PaymentMethod.Saldofavor:
                                        report.Treasury.TotalInternalBalanceUsed += tx.Amount;
                                        break;

                                    case PaymentMethod.Transfer:
                                        report.Treasury.TotalTransfersReceived += tx.Amount;

                                        // 🏦 DESGLOSE BANCARIO AUTOMÁTICO
                                        string bankKey = tx.BankMethod.HasValue
                                            ? tx.BankMethod.Value.ToString()
                                            : "Other";

                                        if (!report.Treasury.BankBreakdown.ContainsKey(bankKey))
                                            report.Treasury.BankBreakdown[bankKey] = 0;

                                        report.Treasury.BankBreakdown[bankKey] += tx.Amount;
                                        break;
                                }
                            }
                        }
                    }
                }

                // C. Agregar Resumen del Jugador
                report.Players.Add(new PlayerReportDto
                {
                    PlayerId = session.PlayerId,
                    // TODO: Conectar con User Service para nombre real
                    PlayerName = "Jugador " + session.PlayerId.ToString().Substring(0, 4),
                    Duration = formattedDuration,
                    BuyIn = pBuyIn,
                    Restaurant = pRest,
                    CashOut = pCashOut,
                    // Fórmula: Lo que se llevó - (Lo que invirtió en juego + lo que comió)
                    NetResult = pCashOut - (pBuyIn + pRest),
                    HasPendingDebt = pHasDebt
                });
            }

            return report;
        }
    }
}
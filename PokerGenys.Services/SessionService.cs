using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;
        private readonly IPlayerRepository _playerRepo; // Nuevo
        private readonly IDealerRepository _dealerRepo; // Nuevo

        public SessionService(
            ISessionRepository repo,
            IPlayerRepository playerRepo,
            IDealerRepository dealerRepo)
        {
            _repo = repo;
            _playerRepo = playerRepo;
            _dealerRepo = dealerRepo;
        }

        public Task<List<Session>> GetAllAsync() => _repo.GetAllActiveAsync();

        public Task<List<Session>> GetAllByTableIdAsync(Guid tableId) => _repo.GetAllByTableIdAsync(tableId);

        public async Task<Session> CreateAsync(Session session)
        {
            if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();

            // LOGICA CRÍTICA: Crear transacción inicial de BuyIn automáticamente
            if (session.InitialBuyIn > 0)
            {
                if (session.Transactions == null) session.Transactions = new List<Transaction>();

                var hasBuyIn = session.Transactions.Any(t => t.Type == TransactionType.BuyIn);

                if (!hasBuyIn)
                {
                    var defaultStatus = PaymentStatus.Pending;
                    var defaultMethod = PaymentMethod.Cash;

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
            // 1. CARGA DE DATOS PARALELA (Optimización de rendimiento)
            var sessionsTask = _repo.GetByTableIdAsync(tableId);
            var playersTask = _playerRepo.GetAllAsync(); // Traemos todos para hacer match en memoria (o usa GetByIds si tienes muchos)
            var dealerShiftsTask = _dealerRepo.GetShiftsAsync(tableId);
            var dealersTask = _dealerRepo.GetAllAsync();

            await Task.WhenAll(sessionsTask, playersTask, dealerShiftsTask, dealersTask);

            var sessions = sessionsTask.Result;
            var allPlayers = playersTask.Result;
            var shifts = dealerShiftsTask.Result;
            var allDealers = dealersTask.Result;

            var report = new TableReportDto();

            // Inicializar desglose bancario
            report.Treasury.BankBreakdown = new Dictionary<string, decimal>
            {
                { "Bancolombia", 0 }, { "Nequi", 0 }, { "Daviplata", 0 }, { "Other", 0 }
            };

            // ---------------------------------------------------------
            // 2. PROCESAMIENTO DE JUGADORES (SESSIONS)
            // ---------------------------------------------------------
            foreach (var session in sessions)
            {
                decimal pBuyIn = 0;
                decimal pRest = 0;
                decimal pCashOut = session.CashOut;
                decimal pCurrentDebt = 0;

                var endTime = session.EndTime ?? DateTime.UtcNow;
                var timeSpan = endTime - session.StartTime;
                string formattedDuration = $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

                // Buscar nombre real del jugador
                var player = allPlayers.FirstOrDefault(p => p.Id == session.PlayerId);
                var playerName = player != null ? $"{player.FirstName} {player.LastName}" : "Jugador Desconocido";

                if (session.Transactions != null)
                {
                    var sortedTransactions = session.Transactions.OrderBy(t => t.CreatedAt);

                    foreach (var tx in sortedTransactions)
                    {
                        // Lógica de Deuda
                        if (tx.PaymentStatus == PaymentStatus.Pending)
                        {
                            pCurrentDebt += tx.Amount;
                        }
                        else if (tx.Type == TransactionType.DebtPayment && tx.PaymentStatus == PaymentStatus.Paid)
                        {
                            pCurrentDebt -= tx.Amount;
                        }

                        // Volumen Operativo
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

                        // Tesorería
                        if (tx.PaymentStatus == PaymentStatus.Paid && tx.Type != TransactionType.CashOut)
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
                                    string bankKey = tx.BankMethod.HasValue ? tx.BankMethod.Value.ToString() : "Other";
                                    if (!report.Treasury.BankBreakdown.ContainsKey(bankKey))
                                        report.Treasury.BankBreakdown[bankKey] = 0;
                                    report.Treasury.BankBreakdown[bankKey] += tx.Amount;
                                    break;
                            }
                        }
                    }
                }

                if (pCurrentDebt < 0) pCurrentDebt = 0;
                report.Treasury.TotalPendingDebt += pCurrentDebt;

                report.Players.Add(new PlayerReportDto
                {
                    PlayerId = session.PlayerId,
                    PlayerName = playerName, // ✅ Nombre Real
                    Duration = formattedDuration,
                    BuyIn = pBuyIn,
                    Restaurant = pRest,
                    CashOut = pCashOut,
                    NetResult = pCashOut - (pBuyIn + pRest),
                    HasPendingDebt = pCurrentDebt > 0,
                    totalMinutes = (int)timeSpan.TotalMinutes
                });
            }

            // ---------------------------------------------------------
            // 3. PROCESAMIENTO DE DEALERS (TURNOS + COSTOS)
            // ---------------------------------------------------------
            // Agrupar turnos por Dealer para sumarizar si tuvo varios turnos en la misma mesa
            var dealerStats = new Dictionary<Guid, DealerReportDto>();

            foreach (var shift in shifts)
            {
                var dealer = allDealers.FirstOrDefault(d => d.Id == shift.DealerId);
                if (dealer == null) continue;

                var shiftEnd = shift.EndTime ?? DateTime.UtcNow;
                var duration = shiftEnd - shift.StartTime;
                var totalHours = duration.TotalHours; // Puede ser decimal (ej: 1.5 horas)

                // Calcular costo: (Horas * Tarifa)
                // Si CostPerHour es null o 0, asumimos 0 costo.
                decimal cost = (decimal)totalHours * (dealer.HourlyRate > 0 ? dealer.HourlyRate : 0);

                if (!dealerStats.ContainsKey(dealer.Id))
                {
                    dealerStats[dealer.Id] = new DealerReportDto
                    {
                        DealerId = dealer.Id,
                        DealerName = dealer.FullName,
                        TotalMinutes = 0,
                        TotalPayable = 0,
                        CostPerHour = dealer.HourlyRate // Para referencia en frontend si se quiere mostrar
                    };
                }

                // Acumular
                dealerStats[dealer.Id].TotalMinutes += (int)duration.TotalMinutes;
                dealerStats[dealer.Id].TotalPayable += cost;
            }

            // Convertir diccionario a lista para el reporte
            report.Dealers = dealerStats.Values.ToList();

            return report;
        }
    }
}
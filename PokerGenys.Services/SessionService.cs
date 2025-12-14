using PokerGenys.Domain.Enums;
using PokerGenys.Domain.Models.Core;      // Para FinancialTransaction
using PokerGenys.Domain.Models.CashGame;  // Para CashSession
using PokerGenys.Domain.DTOs.Audit;       // Para CashAuditResult
using PokerGenys.Domain.DTOs.Reports;     // Para TableReportDto
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;
        private readonly IPlayerRepository _playerRepo;
        private readonly IDealerRepository _dealerRepo;

        public SessionService(
            ISessionRepository repo,
            IPlayerRepository playerRepo,
            IDealerRepository dealerRepo)
        {
            _repo = repo;
            _playerRepo = playerRepo;
            _dealerRepo = dealerRepo;
        }

        // =============================================================
        // 1. CRUD BÁSICO
        // =============================================================
        public Task<List<CashSession>> GetAllAsync() => _repo.GetAllActiveAsync();

        public Task<List<CashSession>> GetAllByTableIdAsync(Guid tableId) => _repo.GetAllByTableIdAsync(tableId);

        public async Task<CashSession> CreateAsync(CashSession session)
        {
            if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();

            // LOGICA CRÍTICA: Crear transacción inicial de BuyIn automáticamente
            if (session.InitialBuyIn > 0) // Usamos InitialStack (nombre nuevo estandarizado) o InitialBuyIn si prefieres
            {
                if (session.Transactions == null) session.Transactions = new List<FinancialTransaction>();

                var hasBuyIn = session.Transactions.Any(t => t.Type == TransactionType.BuyIn);

                if (!hasBuyIn)
                {
                    // Asumimos efectivo por defecto si es automático
                    session.Transactions.Add(new FinancialTransaction
                    {
                        Id = Guid.NewGuid(),
                        WorkingDayId = session.WorkingDayId,
                        Source = TransactionSource.CashGame,
                        SourceId = session.TableId,
                        PlayerId = session.PlayerId,
                        Type = TransactionType.BuyIn,
                        Amount = session.InitialBuyIn,
                        PaymentMethod = PaymentMethod.Cash,
                        Status = PaymentStatus.Paid,
                        Timestamp = DateTime.UtcNow,
                        Description = "Initial BuyIn (Auto-generated)"
                    });
                }
            }

            session.StartTime = DateTime.UtcNow;
            return await _repo.CreateAsync(session);
        }

        public async Task<CashSession?> UpdateAsync(CashSession session)
        {
            var existing = await _repo.GetByIdAsync(session.Id);
            if (existing == null) return null;
            await _repo.UpdateAsync(session);
            return session;
        }

        // =============================================================
        // 2. REPORTES DE MESA (Para el Frontend en tiempo real)
        // =============================================================
        public async Task<TableReportDto> GetTableReportAsync(Guid tableId)
        {
            // 1. CARGA DE DATOS PARALELA
            var sessionsTask = _repo.GetByTableIdAsync(tableId);
            var playersTask = _playerRepo.GetAllAsync(); // Optimizar en producción
            var dealerShiftsTask = _dealerRepo.GetShiftsAsync(tableId);
            var dealersTask = _dealerRepo.GetAllAsync();

            await Task.WhenAll(sessionsTask, playersTask, dealerShiftsTask, dealersTask);

            var sessions = sessionsTask.Result;
            var allPlayers = playersTask.Result;
            var shifts = dealerShiftsTask.Result;
            var allDealers = dealersTask.Result;

            var report = new TableReportDto { TableId = tableId };

            // ---------------------------------------------------------
            // A. PROCESAMIENTO DE JUGADORES
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

                var player = allPlayers.FirstOrDefault(p => p.Id == session.PlayerId);
                var playerName = player?.DisplayName ?? "Jugador Desconocido";

                if (session.Transactions != null)
                {
                    foreach (var tx in session.Transactions)
                    {
                        // Lógica de Deuda
                        if (tx.Status == PaymentStatus.Pending)
                        {
                            pCurrentDebt += tx.Amount;
                        }
                        else if (tx.Type == TransactionType.DebtPayment && tx.Status == PaymentStatus.Paid)
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
                            case TransactionType.ServiceSale:
                                report.TotalRestaurantSales += tx.Amount;
                                pRest += tx.Amount;
                                break;
                            case TransactionType.CashOut:
                                report.TotalCashOuts += tx.Amount;
                                break;
                        }

                        // Tesorería
                        if (tx.Status == PaymentStatus.Paid && tx.Type != TransactionType.CashOut)
                        {
                            switch (tx.PaymentMethod)
                            {
                                case PaymentMethod.Cash:
                                    report.Treasury.TotalCashReceived += tx.Amount;
                                    break;
                                case PaymentMethod.Courtesy:
                                    report.Treasury.TotalCourtesies += tx.Amount;
                                    break;
                                case PaymentMethod.Transfer:
                                    report.Treasury.TotalTransfersReceived += tx.Amount;
                                    string bankKey = tx.Bank.HasValue ? tx.Bank.ToString() : "Other";
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
                    PlayerName = playerName,
                    Duration = formattedDuration,
                    BuyIn = pBuyIn,
                    Restaurant = pRest,
                    CashOut = pCashOut,
                    NetResult = pCashOut - (pBuyIn + pRest),
                    HasPendingDebt = pCurrentDebt > 0,
                    TotalMinutes = (int)timeSpan.TotalMinutes
                });
            }

            // ---------------------------------------------------------
            // B. PROCESAMIENTO DE DEALERS
            // ---------------------------------------------------------
            var dealerStats = new Dictionary<Guid, DealerReportDto>();

            foreach (var shift in shifts)
            {
                var dealer = allDealers.FirstOrDefault(d => d.Id == shift.DealerId);
                if (dealer == null) continue;

                var shiftEnd = shift.EndTime ?? DateTime.UtcNow;
                var duration = shiftEnd - shift.StartTime;
                decimal cost = (decimal)duration.TotalHours * dealer.HourlyRate;

                if (!dealerStats.ContainsKey(dealer.Id))
                {
                    dealerStats[dealer.Id] = new DealerReportDto
                    {
                        DealerId = dealer.Id,
                        DealerName = dealer.FullName,
                        TotalMinutes = 0,
                        TotalPayable = 0,
                        CostPerHour = dealer.HourlyRate
                    };
                }

                dealerStats[dealer.Id].TotalMinutes += (int)duration.TotalMinutes;
                dealerStats[dealer.Id].TotalPayable += cost;
            }

            report.Dealers = dealerStats.Values.ToList();
            return report;
        }

        // =============================================================
        // 3. AUDITORÍA FINANCIERA (Para Cierre de Caja)
        // =============================================================
        public async Task<CashAuditResult> GetFinancialAuditAsync(Guid workingDayId)
        {
            var sessions = await _repo.GetByDayIdAsync(workingDayId);
            var audit = new CashAuditResult();

            foreach (var session in sessions)
            {
                if (session.Transactions == null) continue;

                foreach (var tx in session.Transactions)
                {
                    // Solo contar transacciones PAGADAS
                    if (tx.Status != PaymentStatus.Paid) continue;

                    // Clasificar por Tipo para volumen bruto
                    switch (tx.Type)
                    {
                        case TransactionType.BuyIn:
                        case TransactionType.ReBuy:
                            audit.TotalBuyIns += tx.Amount;
                            break;
                        case TransactionType.CashOut:
                            audit.TotalCashOuts += tx.Amount;
                            break;
                    }

                    // Desglose por Método (Para Arqueo de Caja)
                    string methodKey = tx.PaymentMethod.ToString();
                    if (!audit.PaymentMethodBreakdown.ContainsKey(methodKey))
                        audit.PaymentMethodBreakdown[methodKey] = 0;

                    if (tx.Type == TransactionType.CashOut)
                        audit.PaymentMethodBreakdown[methodKey] -= tx.Amount;
                    else
                        audit.PaymentMethodBreakdown[methodKey] += tx.Amount;
                }
            }

            // Rake estimado (Ajusta el porcentaje según tu regla de negocio, ej. 5%)
            audit.TotalRakeGenerated = audit.TotalBuyIns * 0.05m;

            return audit;
        }
    }
}
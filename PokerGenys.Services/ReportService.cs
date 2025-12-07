using PokerGenys.Domain.Models.Reports  ;
using PokerGenys.Infrastructure.Repositories;
// 👇 ALIAS CLAVE PARA EVITAR CONFLICTOS
using Cash = PokerGenys.Domain.Models.CashGame;
using Shared = PokerGenys.Domain.Models; // Para modelos compartidos si los hay
using Tourney = PokerGenys.Domain.Models.Tournaments;

namespace PokerGenys.Services
{
    public class ReportService : IReportService
    {
        private readonly ISessionRepository _sessionRepo;
        private readonly ITournamentRepository _tournamentRepo;
        private readonly IWorkingDayRepository _workingDayRepo;

        public ReportService(
        ISessionRepository sessionRepo,
        ITournamentRepository tournamentRepo,
        IWorkingDayRepository workingDayRepo) // <--- Inyección
        {
            _sessionRepo = sessionRepo;
            _tournamentRepo = tournamentRepo;
            _workingDayRepo = workingDayRepo;
        }

        public async Task<DailyReportDto> GetDailyReportAsync(Guid workingDayId)
        {
            // 1. OBTENER DATOS (Usando el método nuevo del repo)
            var sessions = await _sessionRepo.GetByDayIdAsync(workingDayId);
            var tournaments = await _tournamentRepo.GetByWorkingDayIdAsync(workingDayId);

            var report = new DailyReportDto
            {
                Date = DateTime.UtcNow,
            };

            // =================================================================
            // PROCESAMIENTO DE CASH GAMES
            // =================================================================
            var tableIds = new HashSet<Guid>();

            foreach (var s in sessions)
            {
                tableIds.Add(s.TableId);
                var end = s.EndTime ?? DateTime.UtcNow;
                report.CashGames.TotalHours += (end - s.StartTime).TotalHours;

                if (s.Transactions != null)
                {
                    foreach (var tx in s.Transactions)
                    {
                        // ✅ USAMOS EL ALIAS 'Cash'
                        if (tx.Type == Cash.TransactionType.BuyIn || tx.Type == Cash.TransactionType.ReBuy)
                            report.CashGames.TotalBuyIns += tx.Amount;

                        if (tx.Type == Cash.TransactionType.Sale)
                            report.RestaurantTotalSales += tx.Amount;

                        if (tx.Type == Cash.TransactionType.CashOut)
                            report.CashGames.TotalCashOuts += tx.Amount;

                        // Mapear al Enum Genérico del DTO o Helper
                        // Convertimos a string y parseamos o pasamos los valores base si coinciden
                        ProcessTreasuryTransaction(
                            report.GlobalTreasury,
                            tx.Type.ToString(), // Pasamos string para evitar líos de tipos
                            tx.Amount,
                            tx.PaymentMethod.ToString(),
                            tx.PaymentStatus.ToString(),
                            tx.BankMethod?.ToString()
                        );
                    }
                }
            }
            report.CashGames.TotalTables = tableIds.Count;
            report.CashGames.GrossProfit = report.CashGames.TotalBuyIns - report.CashGames.TotalCashOuts;

            // =================================================================
            // PROCESAMIENTO DE TORNEOS
            // =================================================================
            report.Tournaments.TotalTournaments = tournaments.Count;

            foreach (var t in tournaments)
            {
                report.Tournaments.TotalEntries += t.TotalEntries;
                report.Tournaments.TotalPrizePool += t.PrizePool;

                decimal tournamentCollected = 0;

                if (t.Transactions != null)
                {
                    foreach (var tx in t.Transactions)
                    {
                        string typeStr = tx.Type.ToString(); // ✅ Convertir a string para comparar fácil

                        // A. Volumen Operativo
                        if (typeStr == "BuyIn" || typeStr == "ReBuy" || typeStr == "AddOn")
                        {
                            tournamentCollected += tx.Amount;
                            if (typeStr == "AddOn") report.Tournaments.TotalAddons++;
                        }

                        // B. Rake y Staff
                        if (typeStr == "HouseRake" || typeStr == "Fee")
                            report.Tournaments.TotalRake += tx.Amount;

                        if (typeStr == "StaffFee")
                            report.Tournaments.TotalStaffFee += tx.Amount;

                        // C. Tesorería
                        if (typeStr != "Payout")
                        {
                            ProcessTreasuryTransaction(
                                report.GlobalTreasury,
                                "BuyIn", // Tratamos todo ingreso como un BuyIn genérico para tesorería
                                tx.Amount,
                                tx.PaymentMethod.ToString(),
                                "Paid", // Asumimos pagado en torneos si existe la tx
                                tx.Bank?.ToString()
                            );
                        }
                    }
                }

                report.Tournaments.TotalCollected += tournamentCollected;

                decimal overlay = t.Guaranteed - tournamentCollected;
                report.Tournaments.Events.Add(new TournamentEventDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Entries = t.TotalEntries,
                    Guaranteed = t.Guaranteed,
                    PrizePool = t.PrizePool,
                    Overlay = overlay > 0 ? overlay : 0,
                    Status = t.Status.ToString() // ✅ Corrección del error CS0029
                });
            }

            // TOTAL FINAL
            report.TotalNetProfit = report.CashGames.GrossProfit
                                  + report.Tournaments.TotalRake
                                  + report.RestaurantTotalSales;

            return report;
        }

        // Helper genérico que recibe STRINGS para ser compatible con ambos mundos
        private void ProcessTreasuryTransaction(
            TreasurySummaryDto treasury,
            string type,
            decimal amount,
            string method,
            string status,
            string? bankName)
        {
            if (status == "Pending")
            {
                treasury.TotalPendingDebt += amount;
                return;
            }

            if (type == "CashOut" || type == "Payout") return;

            switch (method)
            {
                case "Cash":
                    treasury.TotalCashReceived += amount;
                    break;
                case "Courtesy":
                    treasury.TotalCourtesies += amount;
                    break;
                case "Saldofavor":
                    treasury.TotalInternalBalanceUsed += amount;
                    break;
                case "Transfer":
                    treasury.TotalTransfersReceived += amount;
                    var key = string.IsNullOrEmpty(bankName) ? "Other" : bankName;
                    if (!treasury.BankBreakdown.ContainsKey(key)) treasury.BankBreakdown[key] = 0;
                    treasury.BankBreakdown[key] += amount;
                    break;
            }
        }

        public async Task<DailyReportDto?> GetDailyReportByDateAsync(DateTime date)
        {
            // 1. Buscamos el ID de la jornada usando la fecha
            var workingDay = await _workingDayRepo.GetByDateAsync(date);

            if (workingDay == null)
                return null; // No se trabajó ese día

            // 2. Reutilizamos la lógica maestra que ya creamos
            return await GetDailyReportAsync(workingDay.Id);
        }
    }
}
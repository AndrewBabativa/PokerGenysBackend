using PokerGenys.Domain.DTOs.Audit;
using PokerGenys.Domain.Enums;
using PokerGenys.Domain.Models.Core; // Importante: Usa el WorkingDay del Core
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class WorkingDayService : IWorkingDayService
    {
        private readonly IWorkingDayRepository _repo;
        private readonly ISessionService _sessionService;
        private readonly ITournamentService _tournamentService;

        public WorkingDayService(
            IWorkingDayRepository repo,
            ISessionService sessionService,
            ITournamentService tournamentService)
        {
            _repo = repo;
            _sessionService = sessionService;
            _tournamentService = tournamentService;
        }

        public Task<List<WorkingDay>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<WorkingDay> CreateAsync(decimal initialCash, string notes)
        {
            var existing = await _repo.GetOpenDayAsync();
            if (existing != null)
                throw new InvalidOperationException("Ya existe una jornada abierta.");

            var day = new WorkingDay
            {
                Id = Guid.NewGuid(),
                StartAt = DateTime.UtcNow,
                Status = WorkingDayStatus.Open,
                InitialCapita = initialCash,
                Notes = notes,
                // Inicializamos en 0 para seguridad
                FinalCapitaDeclared = 0,
                SystemExpectedCash = 0,
                CashVariance = 0,
                OperationalExpenses = 0
            };
            return await _repo.CreateAsync(day);
        }

        public async Task<WorkingDay> CloseDayAsync(Guid id, decimal finalCount, decimal expenses, string notes)
        {
            var day = await _repo.GetByIdAsync(id);
            if (day == null) throw new KeyNotFoundException("Jornada no encontrada.");
            if (day.Status != WorkingDayStatus.Open)
                throw new InvalidOperationException("Esta jornada ya está cerrada.");

            // =========================================================
            // 1. AUDITORÍA (Consultar subsistemas Cash y Torneos)
            // =========================================================
            var cashAuditTask = _sessionService.GetFinancialAuditAsync(day.Id);
            var tourneyAuditTask = _tournamentService.GetFinancialAuditAsync(day.Id);

            await Task.WhenAll(cashAuditTask, tourneyAuditTask);

            var cashAudit = cashAuditTask.Result;
            var tourneyAudit = tourneyAuditTask.Result;

            // =========================================================
            // 2. GUARDAR SNAPSHOTS (Histórico) - CORREGIDO
            // =========================================================

            // --- Cash Games ---
            day.CashGameBuyIns = cashAudit.TotalBuyIns;
            day.CashGameCashOuts = cashAudit.TotalCashOuts;
            day.CashGameRake = cashAudit.TotalRakeGenerated;

            // --- Torneos (CORRECCIÓN DE NOMBRES) ---
            // Usamos las propiedades del nuevo modelo Core.WorkingDay

            // Total recaudado (BuyIns + Rebuys + Addons)
            day.TournamentCollected = tourneyAudit.TotalCollected;

            // Total pagado en premios
            day.TournamentPayouts = tourneyAudit.TotalPayouts;

            // Ganancia neta (Fees/Rake)
            day.TournamentNetProfit = tourneyAudit.TotalFeesGenerated;

            // --- Gastos y Cierre ---
            day.OperationalExpenses = expenses;
            day.FinalCapitaDeclared = finalCount;
            day.EndAt = DateTime.UtcNow;

            // =========================================================
            // 3. CÁLCULO DE FLUJO DE CAJA (Solo Efectivo Físico)
            // =========================================================
            // Obtenemos solo lo que entró como 'Cash' en el desglose

            decimal cashFromTables = 0;
            if (cashAudit.PaymentMethodBreakdown.ContainsKey("Cash"))
                cashFromTables = cashAudit.PaymentMethodBreakdown["Cash"];

            decimal cashFromTourneys = 0;
            if (tourneyAudit.PaymentMethodBreakdown.ContainsKey("Cash"))
                cashFromTourneys = tourneyAudit.PaymentMethodBreakdown["Cash"];

            // Fórmula: Base Inicial + Neto Efectivo Mesas + Neto Efectivo Torneos - Gastos
            day.SystemExpectedCash = day.InitialCapita
                                   + cashFromTables
                                   + cashFromTourneys
                                   - day.OperationalExpenses;

            // =========================================================
            // 4. VARIANZA
            // =========================================================
            day.CashVariance = day.FinalCapitaDeclared - day.SystemExpectedCash;
            day.Status = WorkingDayStatus.Closed;

            day.Notes = string.IsNullOrWhiteSpace(day.Notes)
                ? notes
                : $"{day.Notes} | Cierre: {notes} | Varianza: {day.CashVariance:C}";

            await _repo.UpdateAsync(day);
            return day;
        }
    }
}
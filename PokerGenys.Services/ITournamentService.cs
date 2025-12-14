using PokerGenys.Domain.DTOs.Audit;
using PokerGenys.Domain.Models.Tournaments;
using PokerGenys.Domain.Models.Core; // ✅ Vital para FinancialTransaction
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ITournamentService
    {
        // --- CRUD BÁSICO ---
        Task<List<Tournament>> GetAllAsync();
        Task<Tournament?> GetByIdAsync(Guid id);
        Task<Tournament> CreateAsync(Tournament tournament);
        Task<Tournament> UpdateAsync(Tournament tournament);
        Task<bool> DeleteAsync(Guid id);

        // --- GESTIÓN DE JUGADORES ---
        Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id);

        // El método AddRegistrationAsync se eliminó para forzar el uso de RegisterPlayerAsync que es transaccional.
        // Si lo necesitas crudo: Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg);

        Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId);

        Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId);

        // --- CONTROL DE JUEGO ---
        Task<Tournament?> StartTournamentAsync(Guid id);
        Task<Tournament?> PauseTournamentAsync(Guid id);
        Task<TournamentState?> GetTournamentStateAsync(Guid id);

        // --- MOVIMIENTOS TRANSACCIONALES ---

        // 1. Registro (Buy-In Inicial)
        Task<RegistrationResult?> RegisterPlayerAsync(
            Guid tournamentId,
            string playerName,
            string paymentMethodStr,
            string? bankStr = null,
            string? reference = null,
            Guid? existingPlayerId = null
        );

        // 2. Recompra (Rebuy)
        Task<RegistrationResult?> RebuyPlayerAsync(
            Guid tournamentId,
            Guid registrationId,
            string paymentMethodStr,
            string? bankStr = null,
            string? reference = null
        );

        // 3. Add-On
        Task<RegistrationResult?> AddOnPlayerAsync(
            Guid tournamentId,
            Guid registrationId,
            string paymentMethodStr,
            string? bankStr = null,
            string? reference = null
        );

        // 4. Ventas Servicios
        Task<FinancialTransaction?> RecordServiceSaleAsync(
            Guid tournamentId,
            Guid? playerId,
            decimal amount,
            string description,
            Dictionary<string, object> items,
            string paymentMethodStr,
            string? bankStr = null,
            string? reference = null
        );

        // 5. Genérico
        Task<FinancialTransaction?> RecordTransactionAsync(
            Guid tournamentId,
            FinancialTransaction transaction
        );

        // --- CONSULTAS ---
        Task<decimal> GetTotalPrizePoolAsync(Guid tournamentId);
        Task<TournamentAuditResult> GetFinancialAuditAsync(Guid workingDayId);
    }
}
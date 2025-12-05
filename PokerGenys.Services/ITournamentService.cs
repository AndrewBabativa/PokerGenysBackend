using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ITournamentService
    {
        // --- CRUD Básico ---
        Task<List<Tournament>> GetAllAsync();
        Task<Tournament?> GetByIdAsync(Guid id);
        Task<Tournament> CreateAsync(Tournament tournament);
        Task<Tournament> UpdateAsync(Tournament tournament);
        Task<bool> DeleteAsync(Guid id);

        // --- Gestión de Jugadores ---
        Task<List<TournamentRegistration>> GetRegistrationsAsync(Guid id);

        Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg);
        Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId);
        Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId);

        // --- Estado y Juego ---
        Task<Tournament?> StartTournamentAsync(Guid id);
        Task<TournamentState?> GetTournamentStateAsync(Guid id);

        // --- MOVIMIENTOS TRANSACCIONALES (Optimizados con Banco/Ref) ---

        // 1. Registro (Buy-In)
        Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName, string paymentMethod, string? bank = null, string? reference = null);

        // 2. Recompra (Rebuy)
        Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null);

        // 3. Add-On (NUEVO: Reglas de negocio para Add-on)
        Task<RegistrationResult?> AddOnPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod, string? bank = null, string? reference = null);

        // 4. Ventas Servicios/Restaurante (NUEVO: Para registrar comida/masajes)
        Task<TournamentTransaction?> RecordServiceSaleAsync(Guid tournamentId, Guid? playerId, decimal amount, string description, Dictionary<string, object> items, string paymentMethod, string? bank = null, string? reference = null);

        // 5. Genérico (Para pagos manuales, ajustes, rake extra)
        Task<TournamentTransaction?> RecordTransactionAsync(Guid tournamentId, TournamentTransaction transaction);

        Task<decimal> GetTotalPrizePoolAsync(Guid tournamentId);
        Task<Tournament?> PauseTournamentAsync(Guid id);

        Task<TournamentStatsDto?> GetTournamentStatsAsync(Guid id);

    }
}
using PokerGenys.Domain.Models;
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

        // Este método suele usarse para carga manual/masiva si es necesario
        Task<Tournament?> AddRegistrationAsync(Guid id, TournamentRegistration reg);

        // ⚠️ CAMBIO CLAVE: Ahora devuelve RemoveResult (con instrucciones de balanceo)
        Task<RemoveResult> RemoveRegistrationAsync(Guid tournamentId, Guid regId);

        Task<TournamentRegistration?> AssignSeatAsync(Guid tournamentId, Guid regId, string tableId, string seatId);

        // --- Estado en Tiempo Real & Lógica Inteligente ---
        Task<Tournament?> StartTournamentAsync(Guid id);
        Task<TournamentState?> GetTournamentStateAsync(Guid id);

        // ⚠️ CAMBIO CLAVE: Ahora devuelve RegistrationResult (con instrucciones de mesa nueva)
        Task<RegistrationResult?> RegisterPlayerAsync(Guid id, string playerName);

        Task<TournamentTransaction?> RecordTransactionAsync(Guid tournamentId, TournamentTransaction transaction);
        Task<decimal> GetTotalPrizePoolAsync(Guid tournamentId);
        Task<RegistrationResult?> RebuyPlayerAsync(Guid tournamentId, Guid registrationId, string paymentMethod);
    }
}
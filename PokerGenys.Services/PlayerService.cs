using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly IPlayerRepository _repo;
        // Podrías inyectar IWaitlistService o ISessionService si necesitas validar dependencias al borrar

        public PlayerService(IPlayerRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Player>> GetAllAsync() => _repo.GetAllAsync();

        public Task<Player?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<Player> CreateAsync(Player player)
        {
            if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();

            player.CreatedAt = DateTime.UtcNow;

            // Inicializar sub-documentos para evitar NullReferenceException
            if (player.Financials == null) player.Financials = new PlayerFinancials();
            if (player.Stats == null) player.Stats = new PlayerStats();

            // Aquí podrías validar duplicados de DocumentId si la base de datos crece

            return await _repo.CreateAsync(player);
        }

        public async Task<Player?> UpdateAsync(Player player)
        {
            var existing = await _repo.GetByIdAsync(player.Id);
            if (existing == null) return null;

            // PROTECCIÓN DE DATOS CRÍTICOS
            // No permitimos que un Update simple sobrescriba las finanzas o estadísticas acumuladas.
            // Esas se deben actualizar solo a través de procesos de cierre de caja o juego.
            player.Financials = existing.Financials;
            player.Stats = existing.Stats;

            player.CreatedAt = existing.CreatedAt;
            player.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(player);
            return player;
        }

        public async Task DeleteAsync(Guid id)
        {
            // Validar si tiene deuda antes de borrar
            var player = await _repo.GetByIdAsync(id);
            if (player != null && player.Financials.TotalDebt > 0)
            {
                throw new InvalidOperationException("No se puede eliminar un jugador con deuda pendiente.");
            }
            await _repo.DeleteAsync(id);
        }
    }
}
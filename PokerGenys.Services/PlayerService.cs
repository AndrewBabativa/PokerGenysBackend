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

        public PlayerService(IPlayerRepository repo) => _repo = repo;

        public Task<List<Player>> GetAllAsync() => _repo.GetAllAsync();

        public Task<Player?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public Task<Player> CreateAsync(Player player)
        {
            if (player.Id == Guid.Empty) player.Id = Guid.NewGuid();
            return _repo.CreateAsync(player);
        }

        public async Task<Player?> UpdateAsync(Player player)
        {
            var existing = await _repo.GetByIdAsync(player.Id);
            if (existing == null) return null;
            await _repo.UpdateAsync(player);
            return player;
        }

        public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
    }
}
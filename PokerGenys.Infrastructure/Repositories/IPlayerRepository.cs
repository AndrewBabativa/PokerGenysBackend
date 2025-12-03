using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IPlayerRepository
    {
        Task<List<Player>> GetAllAsync();
        Task<Player?> GetByIdAsync(Guid id);
        Task<Player> CreateAsync(Player player);
        Task UpdateAsync(Player player);
        Task DeleteAsync(Guid id);
    }
}
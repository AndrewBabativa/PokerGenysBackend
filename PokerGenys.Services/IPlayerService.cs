using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IPlayerService
    {
        Task<List<Player>> GetAllAsync();
        Task<Player?> GetByIdAsync(Guid id);
        Task<Player> CreateAsync(Player player);
        Task<Player?> UpdateAsync(Player player);
        Task DeleteAsync(Guid id);
    }
}
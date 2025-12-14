using PokerGenys.Domain.Models.Core;

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
using PokerGenys.Domain.Models.Core;

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
using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IWaitlistRepository
    {
        Task<List<WaitlistItem>> GetAllAsync();
        Task<List<WaitlistItem>> GetByTableAsync(Guid tableId);
        Task<WaitlistItem> AddAsync(WaitlistItem item);
        Task DeleteAsync(Guid id);
        Task<WaitlistItem?> GetByIdAsync(Guid id);
    }
}
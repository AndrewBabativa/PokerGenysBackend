using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface IWaitlistService
    {
        Task<List<WaitlistItem>> GetAllAsync();
        Task<List<WaitlistItem>> GetByTableAsync(Guid tableId);
        Task<WaitlistItem> AddToWaitlistAsync(Guid tableId, Guid playerId);
        Task RemoveFromWaitlistAsync(Guid id);
        Task<Session?> SeatPlayerAsync(Guid waitlistItemId);
    }
}
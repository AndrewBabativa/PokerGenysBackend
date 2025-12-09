using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface ISessionRepository
    {
        Task<List<Session>> GetAllActiveAsync();
        Task<List<Session>> GetByDayAsync(Guid dayId);
        Task<Session?> GetByIdAsync(Guid id);
        Task<Session> CreateAsync(Session session);
        Task UpdateAsync(Session session);

        Task<List<Session>> GetByTableIdAsync(Guid tableId);

        Task<List<Session>> GetByDayIdAsync(Guid dayId);

        Task<List<Session>> GetAllByTableIdAsync(Guid tableId);
    }
}
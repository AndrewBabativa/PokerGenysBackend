using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ISessionService
    {
        Task<List<Session>> GetAllAsync();
        Task<Session> CreateAsync(Session session);
        Task<Session?> UpdateAsync(Session session);
    }
}
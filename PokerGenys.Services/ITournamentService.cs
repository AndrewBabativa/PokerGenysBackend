// Services/ITournamentService.cs
using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public interface ITournamentService
    {
        Task<List<Tournament>> GetAllAsync();
        Task<Tournament?> GetByIdAsync(Guid id);
        Task<Tournament> CreateAsync(Tournament tournament);
        Task<Tournament> UpdateAsync(Tournament tournament);
        Task<bool> DeleteAsync(Guid id);
    }
}

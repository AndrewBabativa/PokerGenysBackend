// Services/TournamentService.cs
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentRepository _repo;
        public TournamentService(ITournamentRepository repo) => _repo = repo;

        public Task<List<Tournament>> GetAllAsync() => _repo.GetAllAsync();
        public Task<Tournament?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
        public Task<Tournament> CreateAsync(Tournament tournament) => _repo.CreateAsync(tournament);
        public Task<Tournament> UpdateAsync(Tournament tournament) => _repo.UpdateAsync(tournament);
        public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);
    }
}

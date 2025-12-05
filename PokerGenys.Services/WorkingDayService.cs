using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class WorkingDayService : IWorkingDayService
    {
        private readonly IWorkingDayRepository _repo;

        public WorkingDayService(IWorkingDayRepository repo) => _repo = repo;

        public Task<List<WorkingDay>> GetAllAsync() => _repo.GetAllAsync();

        public async Task<WorkingDay> CreateAsync()
        {
            var existingOpen = await _repo.GetOpenDayAsync();
            if (existingOpen != null) return existingOpen;

            var newDay = new WorkingDay
            {
                Id = Guid.NewGuid(),
                StartAt = DateTime.UtcNow,
                Status = WorkingDayStatus.Open
            };
            return await _repo.CreateAsync(newDay);
        }

        public async Task CloseDayAsync(Guid id)
        {
            var day = await _repo.GetByIdAsync(id);
            if (day != null && day.Status == WorkingDayStatus.Open)
            {
                day.Status = WorkingDayStatus.Closed;
                day.ClosedAt = DateTime.UtcNow;
                day.EndAt = DateTime.UtcNow;
                await _repo.UpdateAsync(day);
            }
        }
    }
}
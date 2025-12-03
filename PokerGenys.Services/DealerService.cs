using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class DealerService : IDealerService
    {
        private readonly IDealerRepository _repo;

        public DealerService(IDealerRepository repo) => _repo = repo;

        public Task<List<Dealer>> GetAllDealersAsync() => _repo.GetAllAsync();

        public Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId) =>
            _repo.GetShiftsAsync(dayId, tableId);

        public Task<DealerShift> AddShiftAsync(DealerShift shift)
        {
            if (shift.Id == Guid.Empty) shift.Id = Guid.NewGuid();
            return _repo.AddShiftAsync(shift);
        }

        public async Task<DealerShift?> UpdateShiftAsync(DealerShift shift)
        {
            var existing = await _repo.GetShiftByIdAsync(shift.Id);
            if (existing == null) return null;
            await _repo.UpdateShiftAsync(shift);
            return shift;
        }
    }
}
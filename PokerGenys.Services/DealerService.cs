using PokerGenys.Domain.Models.Core; 
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class DealerService : IDealerService
    {
        private readonly IDealerRepository _repo;

        public DealerService(IDealerRepository repo)
        {
            _repo = repo;
        }

        public Task<List<Dealer>> GetAllDealersAsync() => _repo.GetAllAsync();

        public Task<Dealer?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<Dealer> CreateAsync(Dealer dealer)
        {
            if (dealer.Id == Guid.Empty) dealer.Id = Guid.NewGuid();
            dealer.CreatedAt = DateTime.UtcNow;
            return await _repo.CreateAsync(dealer);
        }

        public async Task<Dealer?> UpdateAsync(Dealer dealer)
        {
            var existing = await _repo.GetByIdAsync(dealer.Id);
            if (existing == null) return null;

            dealer.CreatedAt = existing.CreatedAt;
            dealer.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(dealer);
            return dealer;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repo.DeleteAsync(id);
        }

        public Task<List<DealerShift>> GetShiftsAsync(Guid tableId) => _repo.GetShiftsAsync(tableId);

        public Task<DealerShift> AddShiftAsync(DealerShift shift)
        {
            if (shift.Id == Guid.Empty) shift.Id = Guid.NewGuid();
            return _repo.AddShiftAsync(shift);
        }

        public async Task<DealerShift?> UpdateShiftAsync(DealerShift shift)
        {
            var existing = await _repo.GetShiftByIdAsync(shift.Id);
            if (existing == null) return null;

            shift.WorkingDayId = existing.WorkingDayId;
            shift.TableId = existing.TableId;
            shift.DealerId = existing.DealerId;
            shift.StartTime = existing.StartTime;
            shift.CreatedAt = existing.CreatedAt;

            if (string.IsNullOrEmpty(shift.Notes)) shift.Notes = existing.Notes;

            await _repo.UpdateShiftAsync(shift);
            return shift;
        }

    }
}
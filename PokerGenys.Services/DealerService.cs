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

        public DealerService(IDealerRepository repo)
        {
            _repo = repo;
        }

        // --- CRUD DEALER ---

        public Task<List<Dealer>> GetAllDealersAsync() => _repo.GetAllAsync();

        public Task<Dealer?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);

        public async Task<Dealer> CreateAsync(Dealer dealer)
        {
            if (dealer.Id == Guid.Empty) dealer.Id = Guid.NewGuid();
            dealer.CreatedAt = DateTime.UtcNow;

            // Aquí podrías validar si ya existe un dealer con el mismo DocumentId

            return await _repo.CreateAsync(dealer);
        }

        public async Task<Dealer?> UpdateAsync(Dealer dealer)
        {
            var existing = await _repo.GetByIdAsync(dealer.Id);
            if (existing == null) return null;

            // Mantenemos la fecha de creación original
            dealer.CreatedAt = existing.CreatedAt;
            dealer.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(dealer);
            return dealer;
        }

        public async Task DeleteAsync(Guid id)
        {
            // Opcional: Validar si tiene turnos activos antes de borrar
            await _repo.DeleteAsync(id);
        }

        // --- SHIFTS (Delegación) ---

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
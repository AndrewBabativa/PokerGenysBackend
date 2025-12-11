using PokerGenys.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public interface IDealerRepository
    {
        // --- CRUD DEALERS (Personas) ---
        Task<List<Dealer>> GetAllAsync();
        Task<Dealer?> GetByIdAsync(Guid id);
        Task<Dealer> CreateAsync(Dealer dealer);
        Task UpdateAsync(Dealer dealer);
        Task DeleteAsync(Guid id);

        // --- SHIFTS (Gestión de Turnos - Ya existente) ---
        Task<List<DealerShift>> GetShiftsAsync(Guid tableId);
        Task<DealerShift> AddShiftAsync(DealerShift shift);
        Task UpdateShiftAsync(DealerShift shift);
        Task<DealerShift?> GetShiftByIdAsync(Guid id);
    }
}
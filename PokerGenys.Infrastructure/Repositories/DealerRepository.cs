using MongoDB.Driver;
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class DealerRepository : IDealerRepository
    {
        private readonly MongoContext _context;

        public DealerRepository(MongoContext context)
        {
            _context = context;
        }

        // =================================================
        // CRUD DEALERS
        // =================================================

        public async Task<List<Dealer>> GetAllAsync()
        {
            // Ordenar por nombre para facilitar listas desplegables
            return await _context.Dealers.Find(_ => true)
                                 .SortBy(d => d.FirstName)
                                 .ToListAsync();
        }

        public async Task<Dealer?> GetByIdAsync(Guid id)
        {
            return await _context.Dealers.Find(d => d.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Dealer> CreateAsync(Dealer dealer)
        {
            await _context.Dealers.InsertOneAsync(dealer);
            return dealer;
        }

        public async Task UpdateAsync(Dealer dealer)
        {
            dealer.UpdatedAt = DateTime.UtcNow;
            await _context.Dealers.ReplaceOneAsync(d => d.Id == dealer.Id, dealer);
        }

        public async Task DeleteAsync(Guid id)
        {
            // Opción: Podrías hacer Soft Delete (marcar como Inactivo) en vez de borrar físico
            await _context.Dealers.DeleteOneAsync(d => d.Id == id);
        }

        // =================================================
        // SHIFTS (Lógica existente)
        // =================================================

        public async Task<List<DealerShift>> GetShiftsAsync(Guid dayId, Guid? tableId)
        {
            var filter = Builders<DealerShift>.Filter.Eq(s => s.DayId, dayId);
            if (tableId.HasValue)
                filter &= Builders<DealerShift>.Filter.Eq(s => s.TableId, tableId.Value);

            return await _context.DealerShifts.Find(filter).ToListAsync();
        }

        public async Task<DealerShift> AddShiftAsync(DealerShift shift)
        {
            await _context.DealerShifts.InsertOneAsync(shift);
            return shift;
        }

        public async Task UpdateShiftAsync(DealerShift shift)
        {
            await _context.DealerShifts.ReplaceOneAsync(s => s.Id == shift.Id, shift);
        }

        public async Task<DealerShift?> GetShiftByIdAsync(Guid id)
        {
            return await _context.DealerShifts.Find(s => s.Id == id).FirstOrDefaultAsync();
        }
    }
}
using MongoDB.Driver; // Soluciona el error CS1061 y CS1660
using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerGenys.Infrastructure.Repositories
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly MongoContext _context;

        public PlayerRepository(MongoContext context)
        {
            _context = context;
        }

        // --- Implementación de GetAllAsync ---
        public async Task<List<Player>> GetAllAsync()
        {
            // El error CS1660 se arregla usando la lambda p => true
            return await _context.Players.Find(_ => true)
                                 .SortBy(p => p.FirstName)
                                 .ThenBy(p => p.LastName)
                                 .ToListAsync();
        }

        // --- Solución Error CS0535: GetByIdAsync ---
        public async Task<Player?> GetByIdAsync(Guid id)
        {
            return await _context.Players.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        // --- Solución Error CS0535: CreateAsync ---
        public async Task<Player> CreateAsync(Player player)
        {
            await _context.Players.InsertOneAsync(player);
            return player;
        }

        // --- Implementación de UpdateAsync ---
        public async Task UpdateAsync(Player player)
        {
            await _context.Players.ReplaceOneAsync(p => p.Id == player.Id, player);
        }

        // --- Solución Error CS0535: DeleteAsync ---
        public async Task DeleteAsync(Guid id)
        {
            await _context.Players.DeleteOneAsync(p => p.Id == id);
        }
    }
}
using MongoDB.Driver;
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

        public PlayerRepository(MongoContext context) => _context = context;

        public async Task<List<Player>> GetAllAsync() =>
            await _context.Players.Find(_ => true).ToListAsync();

        public async Task<Player?> GetByIdAsync(Guid id) =>
            await _context.Players.Find(p => p.Id == id).FirstOrDefaultAsync();

        public async Task<Player> CreateAsync(Player player)
        {
            await _context.Players.InsertOneAsync(player);
            return player;
        }

        public async Task UpdateAsync(Player player) =>
            await _context.Players.ReplaceOneAsync(p => p.Id == player.Id, player);

        public async Task DeleteAsync(Guid id) =>
            await _context.Players.DeleteOneAsync(p => p.Id == id);
    }
}
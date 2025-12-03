using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class WaitlistService : IWaitlistService
    {
        private readonly IWaitlistRepository _waitlistRepo;
        private readonly IPlayerRepository _playerRepo;
        private readonly ITableRepository _tableRepo;
        private readonly ISessionService _sessionService; // Inyectamos el servicio de sesión para reusar lógica

        public WaitlistService(
            IWaitlistRepository waitlistRepo,
            IPlayerRepository playerRepo,
            ITableRepository tableRepo,
            ISessionService sessionService)
        {
            _waitlistRepo = waitlistRepo;
            _playerRepo = playerRepo;
            _tableRepo = tableRepo;
            _sessionService = sessionService;
        }

        public Task<List<WaitlistItem>> GetAllAsync() => _waitlistRepo.GetAllAsync();
        public Task<List<WaitlistItem>> GetByTableAsync(Guid tableId) => _waitlistRepo.GetByTableAsync(tableId);

        public async Task<WaitlistItem> AddToWaitlistAsync(Guid tableId, Guid playerId)
        {
            var player = await _playerRepo.GetByIdAsync(playerId);
            if (player == null) throw new Exception("Player not found");

            var currentList = await _waitlistRepo.GetByTableAsync(tableId);
            int nextPriority = currentList.Count > 0 ? currentList.Max(x => x.Priority) + 1 : 1;

            var item = new WaitlistItem
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                PlayerId = playerId,
                PlayerName = player.FirstName + " " + player.LastName,
                Priority = nextPriority,
                RegisteredAt = DateTime.UtcNow
            };

            return await _waitlistRepo.AddAsync(item);
        }

        public Task RemoveFromWaitlistAsync(Guid id) => _waitlistRepo.DeleteAsync(id);

        public async Task<Session?> SeatPlayerAsync(Guid waitlistItemId)
        {
            // 1. Obtener item de lista de espera
            var item = await _waitlistRepo.GetByIdAsync(waitlistItemId);
            if (item == null) return null;

            // 2. Obtener mesa para configuración (BuyIn sugerido)
            var table = await _tableRepo.GetByIdAsync(item.TableId);
            if (table == null) return null;

            // 3. Crear el objeto Sesión
            var newSession = new Session
            {
                Id = Guid.NewGuid(),
                DayId = table.DayId,
                TableId = table.Id,
                PlayerId = item.PlayerId,
                InitialBuyIn = table.InitialBuyInBase, // Sugerido del default de la mesa
                Stack = table.InitialBuyInBase,
                StartTime = DateTime.UtcNow,
                Transactions = new List<Transaction>()
            };

            // 4. Delegar creación al SessionService (esto crea la TX de BuyIn auto)
            var createdSession = await _sessionService.CreateAsync(newSession);

            // 5. Eliminar de la lista de espera
            await _waitlistRepo.DeleteAsync(waitlistItemId);

            return createdSession;
        }
    }
}
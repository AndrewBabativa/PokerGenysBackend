using PokerGenys.Domain.Models;
using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Infrastructure.Repositories;

namespace PokerGenys.Services
{
    public class SessionService : ISessionService
    {
        private readonly ISessionRepository _repo;

        public SessionService(ISessionRepository repo) => _repo = repo;

        public Task<List<Session>> GetAllAsync() => _repo.GetAllActiveAsync();

        public async Task<Session> CreateAsync(Session session)
        {
            if (session.Id == Guid.Empty) session.Id = Guid.NewGuid();

            // LOGICA CRÍTICA: Crear transacción inicial de BuyIn automáticamente
            if (session.InitialBuyIn > 0)
            {
                var hasBuyIn = session.Transactions != null && session.Transactions.Any(t => t.Type == TransactionType.BuyIn);

                if (!hasBuyIn)
                {
                    if (session.Transactions == null) session.Transactions = new List<Transaction>();

                    session.Transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        SessionId = session.Id,
                        Type = TransactionType.BuyIn,
                        Amount = session.InitialBuyIn,
                        PaymentStatus = PaymentStatus.Paid,
                        PaymentMethod = PaymentMethod.Cash,
                        CreatedAt = DateTime.UtcNow,
                        Description = "Initial BuyIn (Auto-generated)"
                    });
                }
            }

            session.TotalInvestment = session.InitialBuyIn;
            session.StartTime = DateTime.UtcNow;

            return await _repo.CreateAsync(session);
        }

        public async Task<Session?> UpdateAsync(Session session)
        {
            var existing = await _repo.GetByIdAsync(session.Id);
            if (existing == null) return null;
            await _repo.UpdateAsync(session);
            return session;
        }
    }
}
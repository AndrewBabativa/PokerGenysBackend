using PokerGenys.Domain.Models.CashGame;
using PokerGenys.Domain.Models.Core;
using PokerGenys.Infrastructure.Repositories;
using System.Text.Json;

namespace PokerGenys.Services
{
    public class CashTableService : ICashTableService
    {
        private readonly ICashTableRepository _repo;

        public CashTableService(ICashTableRepository repo) => _repo = repo;

        public Task<List<CashTable>> GetByDayAsync(Guid workingDayId) => _repo.GetByDayAsync(workingDayId);

        public Task<CashTable> CreateAsync(CashTable table)
        {
            if (table.Id == Guid.Empty) table.Id = Guid.NewGuid();
            return _repo.CreateAsync(table);
        }

        public async Task<CashTable?> UpdateAsync(CashTable incoming)
        {
            var existing = await _repo.GetByIdAsync(incoming.Id);
            if (existing == null) return null;
            if (incoming.Status != default) existing.Status = incoming.Status;
            if (incoming.ClosedAt.HasValue) existing.ClosedAt = incoming.ClosedAt;

            existing.TotalRake = incoming.TotalRake;
            existing.CloseNotes = incoming.CloseNotes;

            if (incoming.Metadata != null)
            {
                if (existing.Metadata == null) existing.Metadata = new Dictionary<string, object>();

                foreach (var item in incoming.Metadata)
                {
                    existing.Metadata[item.Key] = UnwrapJsonElement(item.Value);
                }
            }

            await _repo.UpdateAsync(existing);
            return existing;
        }

        private object UnwrapJsonElement(object value)
        {
            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        if (element.TryGetInt32(out int i)) return i;
                        if (element.TryGetInt64(out long l)) return l;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    default:
                        return element.ToString();
                }
            }
            return value; 
        }
    }
}
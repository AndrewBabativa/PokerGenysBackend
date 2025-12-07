using PokerGenys.Domain.Models;
using PokerGenys.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokerGenys.Services
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _repo;

        public TableService(ITableRepository repo) => _repo = repo;

        public Task<List<TableInstance>> GetByDayAsync(Guid dayId) => _repo.GetByDayAsync(dayId);

        public Task<TableInstance> CreateAsync(TableInstance table)
        {
            if (table.Id == Guid.Empty) table.Id = Guid.NewGuid();
            return _repo.CreateAsync(table);
        }

        public async Task<TableInstance?> UpdateAsync(TableInstance incoming)
        {
            var existing = await _repo.GetByIdAsync(incoming.Id);
            if (existing == null) return null;

            // Actualización de campos básicos
            if (incoming.Status != default) existing.Status = incoming.Status;
            if (incoming.ClosedAt.HasValue) existing.ClosedAt = incoming.ClosedAt;

            existing.TotalRake = incoming.TotalRake;
            existing.CloseNotes = incoming.CloseNotes;

            // 🔥 CORRECCIÓN CRÍTICA PARA METADATA 🔥
            if (incoming.Metadata != null)
            {
                if (existing.Metadata == null) existing.Metadata = new Dictionary<string, object>();

                foreach (var item in incoming.Metadata)
                {
                    // Aquí está la magia: Convertimos el JsonElement a un valor real
                    existing.Metadata[item.Key] = UnwrapJsonElement(item.Value);
                }
            }

            await _repo.UpdateAsync(existing);
            return existing;
        }

        // 👇 AGREGA ESTE MÉTODO PRIVADO AL FINAL DE TU CLASE
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
                    // Agrega más casos si envías arrays u objetos complejos
                    default:
                        return element.ToString();
                }
            }
            return value; // Si no es JsonElement, lo devolvemos tal cual
        }
    }
}
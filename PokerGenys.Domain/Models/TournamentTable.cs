using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PokerGenys.Domain.Models
{
    public class TournamentTable
    {
        // Generamos ID automático para poder rastrear movimientos de jugadores
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // IMPORTANTE PARA VISUALIZACIÓN EN TV ("Mesa 1", "Mesa 2")
        public int TableNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        // "Active", "Broken" (Colapsada), "FinalTable"
        public string Status { get; set; } = "Active";

        public DateTime? ClosedAt { get; set; }

        // Opcional: Para saber quién reparte si usas tablets, 
        // pero en torneos manuales no suele ser crítico guardar esto en BD.
        public Guid? CurrentDealerId { get; set; }

        // Helper para saber cuántos asientos tiene esta mesa específica
        // (Útil si una mesa queda de 8 y otra de 9)
        public int MaxSeats { get; set; } = 9;

    }
}
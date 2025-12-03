using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models
{
    // Enum para el estado del empleado
    public enum DealerStatus
    {
        Active,     // Puede trabajar
        Inactive,   // Ya no trabaja aquí
        Suspended,  // Sancionado
        OnLeave     // Vacaciones o incapacidad
    }

    public class Dealer
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        // --- Identidad ---
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // El apodo es vital en el póker, así los llaman los jugadores
        public string? Nickname { get; set; }

        // Documento de identidad (DNI, Cédula, Pasaporte)
        public string DocumentId { get; set; } = string.Empty;

        // --- Contacto ---
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        // --- Datos Laborales ---
        [BsonRepresentation(BsonType.String)]
        public DealerStatus Status { get; set; } = DealerStatus.Active;

        public DateTime HireDate { get; set; } = DateTime.UtcNow;

        // Tarifa por hora (útil para calcular nómina basada en DealerShifts)
        public decimal HourlyRate { get; set; } = 0;

        // URL de la foto (para mostrar en la app cuando se asigna a mesa)
        public string? PhotoUrl { get; set; }

        // --- Sistema ---
        // Metadatos flexibles (ej: "Talla de uniforme", "Alergias")
        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Propiedad calculada útil para el frontend (no se guarda en BD si no se quiere, o se ignora)
        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}
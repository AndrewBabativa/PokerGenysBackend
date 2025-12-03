using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models
{
    // Estado del jugador en el club
    public enum PlayerStatus
    {
        Active,     // Puede jugar
        Banned,     // Expulsado (por conducta o deuda)
        Inactive,   // No ha venido en X tiempo (útil para marketing)
        VIP         // Jugador de alto valor
    }

    // Clasificación interna (Opcional, para reportes de "quién trae el dinero")
    public enum PlayerType
    {
        Standard,
        Regular,    // Viene todos los días (Grinder)
        Whale,      // Gasta mucho (Recreacional con dinero)
        Nit         // Juega pocas manos (Conservador)
    }

    public class Player
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        // --- 1. IDENTIDAD (Básica y Obligatoria) ---
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } // Opcional si solo quieres manejar un nombre

        // El Nickname es vital en póker para las pantallas de torneos/mesas
        public string? Nickname { get; set; }

        // Cédula/DNI/Pasaporte (Clave para seguridad y deudas)
        public string? DocumentId { get; set; }

        public string? PhotoUrl { get; set; } // Para mostrar en la tablet del dealer

        // --- 2. CONTACTO (Para CRM y Cobranzas) ---
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; } // Para envíos o verificación
        public DateTime? BirthDate { get; set; } // Para bonos de cumpleaños 🎂

        // --- 3. ESTADO Y CLASIFICACIÓN ---
        [BsonRepresentation(BsonType.String)]
        public PlayerStatus Status { get; set; } = PlayerStatus.Active;

        [BsonRepresentation(BsonType.String)]
        public PlayerType Type { get; set; } = PlayerType.Standard;

        // Notas internas del staff (ej: "No prestar dinero", "Le gusta el whisky")
        public string? InternalNotes { get; set; }

        // --- 4. FINANZAS Y ESTADÍSTICAS (Resumen embebido) ---
        // Estos campos se actualizan automáticamente tras cada sesión.
        // Evita tener que sumar todas las transacciones cada vez que listas jugadores.

        public PlayerFinancials Financials { get; set; } = new PlayerFinancials();
        public PlayerStats Stats { get; set; } = new PlayerStats();

        // --- 5. SISTEMA ---
        // Metadatos flexibles para integraciones futuras sin migrar BD
        public Dictionary<string, object>? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Helper para el Frontend (Nombre completo o Nickname si existe)
        [BsonIgnore]
        public string DisplayName => !string.IsNullOrEmpty(Nickname) ? Nickname : $"{FirstName} {LastName}".Trim();
    }

    // Sub-documento para Finanzas (Embebido)
    public class PlayerFinancials
    {
        // Saldo a favor (Créditos comprados y no usados)
        public decimal CreditBalance { get; set; } = 0;

        // Deuda acumulada (Fiaos)
        public decimal TotalDebt { get; set; } = 0;

        // Total histórico gastado (Para calcular VIP)
        public decimal TotalRakeGenerated { get; set; } = 0;
    }

    // Sub-documento para Estadísticas de Juego (Embebido)
    public class PlayerStats
    {
        public int TotalSessionsPlayed { get; set; } = 0;
        public int TotalTournamentsPlayed { get; set; } = 0;
        public int TournamentsWon { get; set; } = 0;

        public DateTime? LastVisit { get; set; }

        // Puntos de fidelidad para canjear por comida/bonos
        public int LoyaltyPoints { get; set; } = 0;
    }
}
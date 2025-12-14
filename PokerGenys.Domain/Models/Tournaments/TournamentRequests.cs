using PokerGenys.Domain.Enums;
using System;
using System.Collections.Generic;

namespace PokerGenys.Domain.Models.Tournaments
{
    // Datos para registrar un jugador
    public class RegisterRequest
    {
        public Guid? PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public PaymentMethod PaymentMethod { get; set; }
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }

    // Datos para Recompra
    public class RebuyRequest
    {
        public Guid RegistrationId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }

    // Datos para Add-On
    public class AddonRequest
    {
        public Guid RegistrationId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }

    // Datos para Asignar Silla
    public class SeatRequest
    {
        public string RegistrationId { get; set; } = string.Empty;
        public string TableId { get; set; } = string.Empty;
        public string SeatId { get; set; } = string.Empty;
    }

    // Datos para Ventas (Restaurante/Servicios)
    public class ServiceSaleRequest
    {
        public Guid? PlayerId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = "Venta";
        public Dictionary<string, string> Items { get; set; } = new();
        public PaymentMethod PaymentMethod { get; set; }
        public string? Bank { get; set; }
        public string? Reference { get; set; }
    }
}
using System.Text.Json.Serialization;

namespace PokerGenys.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WorkingDayStatus { Open, Closing, Closed, Audited }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PlayerStatus { Active, Banned, Inactive, VIP }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PlayerType { Standard, Regular, Whale, Nit, Guest }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DealerStatus { Active, Inactive, Suspended, OnLeave }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TableStatus { Open, Closed }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentStatus { Scheduled, LateRegistration, Running, Paused, Finished, Canceled }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationStatus { Active, Eliminated, Unregistered }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TournamentTableStatus { Active, Broken, FinalTable, Paused, Finished }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RegistrationType { Standard, SatelliteWinner, Invitation, Reentry }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BountyType { Standard, Progressive, Mystery }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeatingMode { Random, Manual, Family }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TableBalancingMode { None, Auto, Strict }
}
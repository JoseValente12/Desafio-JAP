using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

/// <summary>What happens on a contract date: a vehicle leaves or comes back.</summary>
public enum MovementKind
{
    Pickup,
    Return
}

/// <summary>One upcoming pickup or return, with the contract it belongs to.</summary>
public record ContractMovement(MovementKind Kind, DateOnly Date, ContractListItem Contract);

/// <summary>How many vehicles use a fuel type, and the share of the fleet.</summary>
public record FuelShare(FuelType Fuel, int Count, int Percent);

/// <summary>
/// Everything the home page shows. Built in one place so the numbers can be unit tested
/// and the controller stays a thin pass-through.
/// </summary>
public record DashboardSummary(
    int TotalVehicles,
    int RentedToday,
    int AvailableToday,
    double OccupancyPercent,
    int TotalCustomers,
    int ActiveContracts,
    IReadOnlyList<ContractListItem> PickupsToday,
    IReadOnlyList<ContractListItem> ReturnsToday,
    IReadOnlyList<ContractMovement> UpcomingMovements,
    IReadOnlyList<FuelShare> FuelBreakdown,
    double? AverageFleetAge);
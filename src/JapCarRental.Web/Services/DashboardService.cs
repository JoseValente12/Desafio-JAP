using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

public class DashboardService : IDashboardService
{
    // How far ahead the "upcoming movements" table looks, starting tomorrow.
    private const int UpcomingDays = 3;

    private readonly IVehicleService _vehicles;
    private readonly ICustomerService _customers;
    private readonly IContractService _contracts;
    private readonly TimeProvider _timeProvider;

    public DashboardService(
        IVehicleService vehicles,
        ICustomerService customers,
        IContractService contracts,
        TimeProvider timeProvider)
    {
        _vehicles = vehicles;
        _customers = customers;
        _contracts = contracts;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardSummary> GetSummaryAsync()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

        // Awaited one after the other: the three services share one DbContext,
        // which does not allow two queries at the same time.
        var vehicles = await _vehicles.GetAllAsync();
        var customers = await _customers.GetAllAsync();
        var contracts = await _contracts.GetAllAsync();

        // Cancelled contracts are history only: they never count as a pickup or a return.
        var live = contracts.Where(c => c.CancelledOn is null).ToList();

        // IsRented already comes from VehicleService, so the dashboard and the vehicle list
        // can never disagree about what "rented today" means.
        var rented = vehicles.Count(v => v.IsRented);

        return new DashboardSummary(
            TotalVehicles: vehicles.Count,
            RentedToday: rented,
            AvailableToday: vehicles.Count - rented,
            OccupancyPercent: OccupancyPercent(rented, vehicles.Count),
            TotalCustomers: customers.Count,
            ActiveContracts: contracts.Count(c => c.Status == ContractStatus.Active),
            PickupsToday: live.Where(c => c.StartDate == today).ToList(),
            ReturnsToday: live.Where(c => c.EndDate == today).ToList(),
            UpcomingMovements: UpcomingMovements(live, today),
            FuelBreakdown: FuelBreakdown(vehicles),
            AverageFleetAge: AverageAge(vehicles, today.Year));
    }

    // An empty fleet is 0%, not a division by zero.
    private static double OccupancyPercent(int rented, int total) =>
        total == 0 ? 0 : Math.Round(rented * 100.0 / total, 1, MidpointRounding.AwayFromZero);

    // Pickups and returns from tomorrow up to UpcomingDays ahead. Today has its own
    // section, so it is left out here. A short contract can appear twice (pickup and return).
    private static List<ContractMovement> UpcomingMovements(IEnumerable<ContractListItem> contracts, DateOnly today)
    {
        var first = today.AddDays(1);
        var last = today.AddDays(UpcomingDays);

        var list = contracts.ToList();

        var pickups = list
            .Where(c => c.StartDate >= first && c.StartDate <= last)
            .Select(c => new ContractMovement(MovementKind.Pickup, c.StartDate, c));

        var returns = list
            .Where(c => c.EndDate >= first && c.EndDate <= last)
            .Select(c => new ContractMovement(MovementKind.Return, c.EndDate, c));

        return pickups.Concat(returns)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Kind)
            .ThenBy(m => m.Contract.VehicleName)
            .ToList();
    }

    private static List<FuelShare> FuelBreakdown(IReadOnlyList<VehicleListItem> vehicles) =>
        vehicles
            .GroupBy(v => v.FuelType)
            .Select(g => new FuelShare(
                g.Key,
                g.Count(),
                (int)Math.Round(g.Count() * 100.0 / vehicles.Count, MidpointRounding.AwayFromZero)))
            .OrderByDescending(f => f.Count)
            .ThenBy(f => f.Fuel)
            .ToList();

    // Null for an empty fleet, so the page can show a dash instead of a made-up zero.
    // The year comes from the clock, never a constant, so the figure ages correctly.
    private static double? AverageAge(IReadOnlyList<VehicleListItem> vehicles, int currentYear) =>
        vehicles.Count == 0
            ? null
            : Math.Round(vehicles.Average(v => currentYear - v.ManufactureYear), 1, MidpointRounding.AwayFromZero);
}
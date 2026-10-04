using JapCarRental.Web.Models;
using JapCarRental.Web.Services;
using Microsoft.Extensions.Time.Testing;

namespace JapCarRental.Tests.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    // "Today" is fixed at 2 October 2026 so every test is deterministic.
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));

    private readonly VehicleService _vehicleService;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _vehicleService = new VehicleService(_database.Context, _time);

        // The dashboard is built on top of the three real services, so these tests also prove
        // that the numbers agree with what the vehicle and contract lists show.
        // If CustomerService takes different constructor arguments, adjust this one line.
        _service = new DashboardService(
            _vehicleService,
            new CustomerService(_database.Context),
            new ContractService(_database.Context, _time),
            _time);
    }

    public void Dispose() => _database.Dispose();

    // Shorthand for dates in October 2026.
    private static DateOnly Oct(int day) => new(2026, 10, day);

    private async Task<int> AddVehicleAsync(string plate, int year = 2021, FuelType fuel = FuelType.Petrol)
    {
        var result = await _vehicleService.CreateAsync(new VehicleInput("Renault", "Clio", plate, year, fuel));
        return result.Value;
    }

    private async Task<int> AddCustomerAsync(string email = "ana@example.com")
    {
        var customer = new Customer("Ana Teste", email, "910000001", "P-1");
        _database.Context.Customers.Add(customer);
        await _database.Context.SaveChangesAsync();
        return customer.Id;
    }

    // Inserts a contract directly, bypassing the service rules, so we can build contracts
    // that are already running or finished.
    private async Task<RentalContract> AddContractAsync(int customerId, int vehicleId, DateOnly start, DateOnly end)
    {
        var contract = new RentalContract(customerId, vehicleId, start, end, 1000);
        _database.Context.RentalContracts.Add(contract);
        await _database.Context.SaveChangesAsync();
        return contract;
    }

    // ---------- Empty database ----------

    [Fact]
    public async Task Summary_IsAllZeros_ForAnEmptyDatabase()
    {
        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalVehicles);
        Assert.Equal(0, summary.RentedToday);
        Assert.Equal(0, summary.AvailableToday);
        Assert.Equal(0, summary.OccupancyPercent); // no division by zero
        Assert.Equal(0, summary.TotalCustomers);
        Assert.Equal(0, summary.ActiveContracts);
        Assert.Empty(summary.PickupsToday);
        Assert.Empty(summary.ReturnsToday);
        Assert.Empty(summary.UpcomingMovements);
        Assert.Empty(summary.FuelBreakdown);
        Assert.Null(summary.AverageFleetAge); // a dash on the page, not a made-up zero
    }

    // ---------- Fleet figures ----------

    [Fact]
    public async Task Occupancy_IsRentedOverTotal()
    {
        var customerId = await AddCustomerAsync();
        var rentedId = await AddVehicleAsync("AA11BB");
        await AddVehicleAsync("CC22DD");
        await AddContractAsync(customerId, rentedId, Oct(2), Oct(5)); // active today

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(2, summary.TotalVehicles);
        Assert.Equal(1, summary.RentedToday);
        Assert.Equal(1, summary.AvailableToday);
        Assert.Equal(50, summary.OccupancyPercent);
    }

    [Fact]
    public async Task Occupancy_IsRoundedToOneDecimal()
    {
        var customerId = await AddCustomerAsync();
        var rentedId = await AddVehicleAsync("AA11BB");
        await AddVehicleAsync("CC22DD");
        await AddVehicleAsync("EE33FF");
        await AddContractAsync(customerId, rentedId, Oct(2), Oct(5));

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(33.3, summary.OccupancyPercent); // 1 of 3
    }

    [Fact]
    public async Task UpcomingContract_DoesNotMakeTheVehicleRentedToday()
    {
        var customerId = await AddCustomerAsync();
        var vehicleId = await AddVehicleAsync("AA11BB");
        await AddContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.RentedToday);
        Assert.Equal(1, summary.AvailableToday);
    }

    [Fact]
    public async Task CancelledContract_IsIgnoredEverywhere()
    {
        var customerId = await AddCustomerAsync();
        var vehicleId = await AddVehicleAsync("AA11BB");
        var contract = await AddContractAsync(customerId, vehicleId, Oct(2), Oct(2));
        contract.Cancel(Oct(2));
        await _database.Context.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.RentedToday);
        Assert.Equal(0, summary.ActiveContracts);
        Assert.Empty(summary.PickupsToday);
        Assert.Empty(summary.ReturnsToday);
    }

    [Fact]
    public async Task TotalCustomers_CountsEveryCustomer()
    {
        await AddCustomerAsync("ana@example.com");
        await AddCustomerAsync("rui@example.com");

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(2, summary.TotalCustomers);
    }

    // ---------- Contract figures ----------

    [Fact]
    public async Task ActiveContracts_CountsOnlyContractsRunningToday()
    {
        var customerId = await AddCustomerAsync();
        var first = await AddVehicleAsync("AA11BB");
        var second = await AddVehicleAsync("CC22DD");
        var third = await AddVehicleAsync("EE33FF");
        await AddContractAsync(customerId, first, Oct(1), Oct(5));    // active
        await AddContractAsync(customerId, second, Oct(10), Oct(15)); // upcoming
        await AddContractAsync(customerId, third, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5)); // finished

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(1, summary.ActiveContracts);
    }

    [Fact]
    public async Task PickupsToday_ListsContractsStartingToday()
    {
        var customerId = await AddCustomerAsync();
        var starting = await AddVehicleAsync("AA11BB");
        var other = await AddVehicleAsync("CC22DD");
        var contract = await AddContractAsync(customerId, starting, Oct(2), Oct(8));
        await AddContractAsync(customerId, other, Oct(3), Oct(8)); // tomorrow, not today

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(contract.Id, Assert.Single(summary.PickupsToday).Id);
    }

    [Fact]
    public async Task ReturnsToday_ListsContractsEndingToday()
    {
        var customerId = await AddCustomerAsync();
        var ending = await AddVehicleAsync("AA11BB");
        var other = await AddVehicleAsync("CC22DD");
        var contract = await AddContractAsync(customerId, ending, Oct(1), Oct(2)); // end date is inclusive
        await AddContractAsync(customerId, other, Oct(1), Oct(3));

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(contract.Id, Assert.Single(summary.ReturnsToday).Id);
    }

    [Fact]
    public async Task SameDayContract_IsBothAPickupAndAReturn()
    {
        var customerId = await AddCustomerAsync();
        var vehicleId = await AddVehicleAsync("AA11BB");
        var contract = await AddContractAsync(customerId, vehicleId, Oct(2), Oct(2));

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(contract.Id, Assert.Single(summary.PickupsToday).Id);
        Assert.Equal(contract.Id, Assert.Single(summary.ReturnsToday).Id);
    }

    // ---------- Upcoming movements (tomorrow up to 3 days ahead) ----------

    [Fact]
    public async Task UpcomingMovements_OnlyIncludeTomorrowUpToThreeDaysAhead()
    {
        var customerId = await AddCustomerAsync();
        var a = await AddVehicleAsync("AA11BB");
        var b = await AddVehicleAsync("CC22DD");
        var c = await AddVehicleAsync("EE33FF");
        var d = await AddVehicleAsync("GG44HH");

        await AddContractAsync(customerId, a, Oct(3), Oct(20)); // pickup on the 3rd is in; return is far away
        await AddContractAsync(customerId, b, Oct(1), Oct(5));  // return on the 5th (today + 3) is in
        await AddContractAsync(customerId, c, Oct(6), Oct(9));  // starts one day after the window
        await AddContractAsync(customerId, d, Oct(2), Oct(2));  // today has its own section

        var summary = await _service.GetSummaryAsync();

        Assert.Collection(summary.UpcomingMovements,
            m =>
            {
                Assert.Equal(MovementKind.Pickup, m.Kind);
                Assert.Equal(Oct(3), m.Date);
            },
            m =>
            {
                Assert.Equal(MovementKind.Return, m.Kind);
                Assert.Equal(Oct(5), m.Date);
            });
    }

    [Fact]
    public async Task UpcomingMovements_ShowAShortContractTwice_OrderedByDate()
    {
        var customerId = await AddCustomerAsync();
        var vehicleId = await AddVehicleAsync("AA11BB");
        await AddContractAsync(customerId, vehicleId, Oct(3), Oct(4));

        var summary = await _service.GetSummaryAsync();

        Assert.Collection(summary.UpcomingMovements,
            m =>
            {
                Assert.Equal(MovementKind.Pickup, m.Kind);
                Assert.Equal(Oct(3), m.Date);
            },
            m =>
            {
                Assert.Equal(MovementKind.Return, m.Kind);
                Assert.Equal(Oct(4), m.Date);
            });
    }

    [Fact]
    public async Task UpcomingMovements_IgnoreCancelledContracts()
    {
        var customerId = await AddCustomerAsync();
        var vehicleId = await AddVehicleAsync("AA11BB");
        var contract = await AddContractAsync(customerId, vehicleId, Oct(3), Oct(4));
        contract.Cancel(Oct(2));
        await _database.Context.SaveChangesAsync();

        var summary = await _service.GetSummaryAsync();

        Assert.Empty(summary.UpcomingMovements);
    }

    // ---------- Fleet profile ----------

    [Fact]
    public async Task FuelBreakdown_CountsAndSortsByMostUsed()
    {
        await AddVehicleAsync("AA11BB", fuel: FuelType.Diesel);
        await AddVehicleAsync("CC22DD", fuel: FuelType.Petrol);
        await AddVehicleAsync("EE33FF", fuel: FuelType.Petrol);

        var summary = await _service.GetSummaryAsync();

        Assert.Collection(summary.FuelBreakdown,
            f =>
            {
                Assert.Equal(FuelType.Petrol, f.Fuel);
                Assert.Equal(2, f.Count);
                Assert.Equal(67, f.Percent);
            },
            f =>
            {
                Assert.Equal(FuelType.Diesel, f.Fuel);
                Assert.Equal(1, f.Count);
                Assert.Equal(33, f.Percent);
            });
    }

    [Fact]
    public async Task AverageFleetAge_UsesTheClockYear()
    {
        // The fixed clock says 2026, so these vehicles are 5 and 3 years old.
        await AddVehicleAsync("AA11BB", year: 2021);
        await AddVehicleAsync("CC22DD", year: 2023);

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(4.0, summary.AverageFleetAge);
    }
}

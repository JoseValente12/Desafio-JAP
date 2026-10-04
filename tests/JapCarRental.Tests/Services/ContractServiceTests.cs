using JapCarRental.Web.Models;
using JapCarRental.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace JapCarRental.Tests.Services;

public class ContractServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    // "Today" is fixed at 2 October 2026 so every test is deterministic.
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));

    private readonly ContractService _service;
    private readonly VehicleService _vehicleService;

    public ContractServiceTests()
    {
        _service = new ContractService(_database.Context, _time);
        _vehicleService = new VehicleService(_database.Context, _time);
    }

    public void Dispose() => _database.Dispose();

    // Shorthand for dates in October 2026.
    private static DateOnly Oct(int day) => new(2026, 10, day);

    // Creates a customer and a vehicle. The vehicle goes through VehicleService so the test
    // does not depend on the Vehicle constructor signature.
    private async Task<(int CustomerId, int VehicleId)> SeedAsync(
        string plate = "AA11BB", string email = "ana@example.com")
    {
        var customer = new Customer("Ana Teste", email, "910000001", "P-1");
        _database.Context.Customers.Add(customer);
        await _database.Context.SaveChangesAsync();

        var vehicle = await _vehicleService.CreateAsync(
            new VehicleInput("Renault", "Clio", plate, 2021, FuelType.Petrol));

        return (customer.Id, vehicle.Value);
    }

    // Inserts a contract directly, bypassing the service rules. Needed to build situations the
    // service refuses to create (contracts already started or finished).
    private async Task<RentalContract> InsertContractAsync(
        int customerId, int vehicleId, DateOnly start, DateOnly end)
    {
        var contract = new RentalContract(customerId, vehicleId, start, end, 1000);
        _database.Context.RentalContracts.Add(contract);
        await _database.Context.SaveChangesAsync();
        return contract;
    }

    private static ContractInput Input(int customerId, int vehicleId, DateOnly start, DateOnly end) =>
        new(customerId, vehicleId, start, end, 1000);

    // ---------- Create: happy path ----------

    [Fact]
    public async Task Create_Succeeds_ForFreeVehicle()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(10), Oct(15)));

        Assert.True(result.Succeeded);
        Assert.Equal(1, await _database.Context.RentalContracts.CountAsync());
    }

    [Fact]
    public async Task Create_Succeeds_WhenStartingToday()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(2), Oct(5)));

        Assert.True(result.Succeeded);
    }

    // ---------- Create: overlap rule ----------

    // Existing contract: 10 to 15 October (both days inclusive).
    // Every case below shares at least one day with it, so all must be rejected.
    [Theory]
    [InlineData(5, 10)]   // ends on the day the existing one starts (inclusive end)
    [InlineData(15, 20)]  // starts on the day the existing one ends
    [InlineData(8, 12)]   // overlaps the beginning
    [InlineData(13, 20)]  // overlaps the end
    [InlineData(12, 13)]  // fully contained
    [InlineData(5, 25)]   // fully contains the existing one
    [InlineData(10, 15)]  // identical period
    public async Task Create_Fails_WhenPeriodOverlapsAnExistingContract(int startDay, int endDay)
    {
        var (customerId, vehicleId) = await SeedAsync();
        await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(startDay), Oct(endDay)));

        Assert.False(result.Succeeded);
        Assert.Contains("VehicleId", result.Errors.Keys);
        Assert.Equal(1, await _database.Context.RentalContracts.CountAsync());
    }

    // These share no day with 10 to 15 October, so they must be accepted.
    [Theory]
    [InlineData(5, 9)]    // ends the day before
    [InlineData(16, 20)]  // starts the day after
    public async Task Create_Succeeds_WhenPeriodIsAdjacentButDoesNotShareADay(int startDay, int endDay)
    {
        var (customerId, vehicleId) = await SeedAsync();
        await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(startDay), Oct(endDay)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Create_Succeeds_WhenAnotherVehicleHasAContractInTheSamePeriod()
    {
        var (customerId, firstVehicleId) = await SeedAsync("AA11BB");
        var secondVehicleId = (await _vehicleService.CreateAsync(
            new VehicleInput("Peugeot", "208", "CC22DD", 2022, FuelType.Diesel))).Value;
        await InsertContractAsync(customerId, firstVehicleId, Oct(10), Oct(15));

        var result = await _service.CreateAsync(Input(customerId, secondVehicleId, Oct(10), Oct(15)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Create_Succeeds_WhenTheOnlyOverlappingContractWasCancelled()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var existing = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));
        existing.Cancel(Oct(2));
        await _database.Context.SaveChangesAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(10), Oct(15)));

        Assert.True(result.Succeeded);
    }

    // ---------- Create: input validation ----------

    [Fact]
    public async Task Create_Fails_WhenStartDateIsInThePast()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(1), Oct(5)));

        Assert.False(result.Succeeded);
        Assert.Contains("StartDate", result.Errors.Keys);
    }

    [Fact]
    public async Task Create_Succeeds_WhenSameDayContract()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(10), Oct(10)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Create_Fails_WhenEndDateIsBeforeStartDate()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, vehicleId, Oct(10), Oct(9)));

        Assert.False(result.Succeeded);
        Assert.Contains("EndDate", result.Errors.Keys);
    }

    [Fact]
    public async Task Create_Fails_WhenInitialMileageIsNegative()
    {
        var (customerId, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(
            Input(customerId, vehicleId, Oct(10), Oct(15)) with { InitialMileage = -1 });

        Assert.False(result.Succeeded);
        Assert.Contains("InitialMileage", result.Errors.Keys);
    }

    [Fact]
    public async Task Create_Fails_WhenCustomerDoesNotExist()
    {
        var (_, vehicleId) = await SeedAsync();

        var result = await _service.CreateAsync(Input(999, vehicleId, Oct(10), Oct(15)));

        Assert.False(result.Succeeded);
        Assert.Contains("CustomerId", result.Errors.Keys);
    }

    [Fact]
    public async Task Create_Fails_WhenVehicleDoesNotExist()
    {
        var (customerId, _) = await SeedAsync();

        var result = await _service.CreateAsync(Input(customerId, 999, Oct(10), Oct(15)));

        Assert.False(result.Succeeded);
        Assert.Contains("VehicleId", result.Errors.Keys);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_Succeeds_ForAgendadoContract()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var result = await _service.UpdateAsync(contract.Id, Input(customerId, vehicleId, Oct(12), Oct(18)));

        Assert.True(result.Succeeded);
        var updated = await _service.GetByIdAsync(contract.Id);
        Assert.Equal(Oct(12), updated!.StartDate);
        Assert.Equal(Oct(18), updated.EndDate);
    }

    [Fact]
    public async Task Update_Fails_WhenContractHasAlreadyStarted()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(2), Oct(5)); // starts today (Oct 2)

        var result = await _service.UpdateAsync(contract.Id, Input(customerId, vehicleId, Oct(10), Oct(15)));

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors.Values.SelectMany(x => x));
        Assert.Equal("Contratos em curso ou finalizados não podem ser alterados.", error);
    }

    [Fact]
    public async Task Update_Fails_WhenContractIsCancelled()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));
        await _service.CancelAsync(contract.Id);

        var result = await _service.UpdateAsync(contract.Id, Input(customerId, vehicleId, Oct(12), Oct(18)));

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors.Values.SelectMany(x => x));
        Assert.Equal("Contratos em curso ou finalizados não podem ser alterados.", error);
    }

    [Fact]
    public async Task Update_Fails_WhenNewPeriodOverlapsAnotherContract()
    {
        var (customerId, firstVehicleId) = await SeedAsync("AA11BB");
        var secondVehicleId = (await _vehicleService.CreateAsync(
            new VehicleInput("Peugeot", "208", "CC22DD", 2022, FuelType.Diesel))).Value;

        // Existing contract on second vehicle from Oct 10 to Oct 15
        await InsertContractAsync(customerId, secondVehicleId, Oct(10), Oct(15));
        // Contract to edit on first vehicle
        var contractToEdit = await InsertContractAsync(customerId, firstVehicleId, Oct(20), Oct(25));

        // Try to update contractToEdit to use secondVehicleId during Oct 12 to Oct 18 (overlaps Oct 10..15)
        var result = await _service.UpdateAsync(contractToEdit.Id, Input(customerId, secondVehicleId, Oct(12), Oct(18)));

        Assert.False(result.Succeeded);
        Assert.Contains("VehicleId", result.Errors.Keys);
    }

    [Fact]
    public async Task Update_Succeeds_WhenKeepingSameVehicleAndPeriod()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var result = await _service.UpdateAsync(contract.Id, Input(customerId, vehicleId, Oct(10), Oct(15)));

        Assert.True(result.Succeeded);
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_Succeeds_BeforeTheContractStarts_AndKeepsTheRow()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var result = await _service.CancelAsync(contract.Id);

        Assert.True(result.Succeeded);

        // Soft cancellation: the row is still there, with the date it was cancelled.
        var saved = await _database.Context.RentalContracts.SingleAsync();
        Assert.Equal(Oct(2), saved.CancelledOn);

        var item = await _service.GetByIdAsync(contract.Id);
        Assert.Equal(ContractStatus.Cancelled, item!.Status);
    }

    [Fact]
    public async Task Cancel_Fails_WhenTheContractHasAlreadyStarted()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(2), Oct(5)); // starts today

        var result = await _service.CancelAsync(contract.Id);

        Assert.False(result.Succeeded);
        Assert.Null((await _database.Context.RentalContracts.SingleAsync()).CancelledOn);
    }

    [Fact]
    public async Task Cancel_Fails_WhenTheContractIsAlreadyCancelled()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));
        await _service.CancelAsync(contract.Id);

        var result = await _service.CancelAsync(contract.Id);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Cancel_Fails_WhenTheContractDoesNotExist()
    {
        var result = await _service.CancelAsync(999);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Cancel_FreesTheVehicleForTheSamePeriod()
    {
        var (customerId, vehicleId) = await SeedAsync();
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));
        Assert.Empty(await _service.GetAvailableVehiclesAsync(Oct(10), Oct(15)));

        await _service.CancelAsync(contract.Id);

        var available = await _service.GetAvailableVehiclesAsync(Oct(10), Oct(15));
        Assert.Single(available);
        Assert.Equal(vehicleId, available.Single().Id);
    }

    // ---------- Vehicle status ----------

    [Fact]
    public async Task CancelledContract_DoesNotMakeTheVehicleRented()
    {
        var (customerId, vehicleId) = await SeedAsync();

        // Active today (2 to 5 October) but cancelled. Built directly because the service
        // does not allow cancelling a contract that has already started.
        var contract = await InsertContractAsync(customerId, vehicleId, Oct(2), Oct(5));
        contract.Cancel(Oct(2));
        await _database.Context.SaveChangesAsync();

        var vehicles = await _vehicleService.GetAllAsync();

        Assert.False(vehicles.Single().IsRented);
    }

    // ---------- Queries ----------

    [Fact]
    public async Task GetAvailableVehicles_ExcludesVehiclesWithAContractInThePeriod()
    {
        var (customerId, bookedVehicleId) = await SeedAsync("AA11BB");
        var freeVehicleId = (await _vehicleService.CreateAsync(
            new VehicleInput("Peugeot", "208", "CC22DD", 2022, FuelType.Diesel))).Value;
        await InsertContractAsync(customerId, bookedVehicleId, Oct(10), Oct(15));

        var available = await _service.GetAvailableVehiclesAsync(Oct(12), Oct(20));

        Assert.Equal(freeVehicleId, available.Single().Id);
    }

    [Fact]
    public async Task GetAvailableVehicles_ReturnsNothing_WhenPeriodIsInvalid()
    {
        await SeedAsync();

        var available = await _service.GetAvailableVehiclesAsync(Oct(15), Oct(10));

        Assert.Empty(available);
    }

    [Fact]
    public async Task GetAll_IncludesCustomerAndVehicleNames()
    {
        var (customerId, vehicleId) = await SeedAsync();
        await InsertContractAsync(customerId, vehicleId, Oct(10), Oct(15));

        var item = (await _service.GetAllAsync()).Single();

        Assert.Equal("Ana Teste", item.CustomerName);
        Assert.Equal("Renault Clio", item.VehicleName);
        Assert.Equal("AA11BB", item.LicensePlate);
    }

    // "Today" is 2 October. The end date is inclusive, so a contract ending today is still active.
    [Theory]
    [InlineData(10, 15, ContractStatus.Upcoming)]
    [InlineData(2, 5, ContractStatus.Active)]    // first day
    [InlineData(1, 2, ContractStatus.Active)]    // last day
    [InlineData(20, 25, ContractStatus.Upcoming)]
    public async Task Status_IsCalculatedFromTheDates(int startDay, int endDay, ContractStatus expected)
    {
        var (customerId, vehicleId) = await SeedAsync();
        await InsertContractAsync(customerId, vehicleId, Oct(startDay), Oct(endDay));

        var item = (await _service.GetAllAsync()).Single();

        Assert.Equal(expected, item.Status);
    }

    [Fact]
    public async Task Status_IsFinished_AfterTheEndDate()
    {
        var (customerId, vehicleId) = await SeedAsync();
        await InsertContractAsync(customerId, vehicleId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5));

        var item = (await _service.GetAllAsync()).Single();

        Assert.Equal(ContractStatus.Finished, item.Status);
    }
}
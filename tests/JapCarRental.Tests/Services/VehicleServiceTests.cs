using JapCarRental.Web.Models;
using JapCarRental.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;

namespace JapCarRental.Tests.Services;

public class VehicleServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));
    private readonly VehicleService _service;

    public VehicleServiceTests()
    {
        _service = new VehicleService(_database.Context, _time);
    }

    public void Dispose() => _database.Dispose();

    private static VehicleInput ValidInput(string plate = "AA11BB") =>
        new("Renault", "Clio", plate, 2021, FuelType.Petrol);

    private sealed class RaceInterceptor(Func<Task> competingInsert) : SaveChangesInterceptor
    {
        private bool _done;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (!_done)
            {
                _done = true;
                await competingInsert();
            }
            return await base.SavingChangesAsync(eventData, result, ct);
        }
    }

    [Fact]
    public async Task Create_ReturnsPlateError_WhenAnotherRequestWinsTheRace()
    {
        var interceptor = new RaceInterceptor(async () =>
        {
            using var other = _database.CreateContext();
            other.Vehicles.Add(new Vehicle("Peugeot", "208", "AA11BB", 2022, FuelType.Diesel));
            await other.SaveChangesAsync();
        });

        using var racedContext = _database.CreateContext(interceptor);
        var service = new VehicleService(racedContext, _time);

        var result = await service.CreateAsync(ValidInput("AA11BB"));

        Assert.False(result.Succeeded);
        Assert.Contains("LicensePlate", result.Errors.Keys);
        Assert.Equal("Já existe um veículo com esta matrícula.", result.Errors["LicensePlate"].Single());
    }

    [Fact]
    public async Task Create_Succeeds_AndNormalizesThePlate()
    {
        var result = await _service.CreateAsync(ValidInput("aa-11 bb"));

        Assert.True(result.Succeeded);
        var saved = await _database.Context.Vehicles.SingleAsync();
        Assert.Equal("AA11BB", saved.LicensePlate);
    }

    [Fact]
    public async Task Create_Fails_WhenPlateAlreadyExists_EvenWithDifferentFormatting()
    {
        await _service.CreateAsync(ValidInput("AA11BB"));

        var result = await _service.CreateAsync(ValidInput("aa-11-bb"));

        Assert.False(result.Succeeded);
        Assert.Contains("LicensePlate", result.Errors.Keys);
        Assert.Equal(1, await _database.Context.Vehicles.CountAsync());
    }

    [Fact]
    public async Task Create_Fails_WhenManufactureYearIsInTheFuture()
    {
        var input = ValidInput() with { ManufactureYear = 2027 };

        var result = await _service.CreateAsync(input);

        Assert.False(result.Succeeded);
        Assert.Contains("ManufactureYear", result.Errors.Keys);
    }

    [Fact]
    public async Task Update_Succeeds_WhenKeepingTheOwnPlate()
    {
        var created = await _service.CreateAsync(ValidInput("AA11BB"));

        var result = await _service.UpdateAsync(created.Value, ValidInput("AA11BB") with { Model = "Megane" });

        Assert.True(result.Succeeded);
        var saved = await _database.Context.Vehicles.SingleAsync();
        Assert.Equal("Megane", saved.Model);
    }

    [Fact]
    public async Task Update_Fails_WhenPlateBelongsToAnotherVehicle()
    {
        await _service.CreateAsync(ValidInput("AA11BB"));
        var second = await _service.CreateAsync(ValidInput("CC22DD"));

        var result = await _service.UpdateAsync(second.Value, ValidInput("AA11BB"));

        Assert.False(result.Succeeded);
        Assert.Contains("LicensePlate", result.Errors.Keys);
    }

    [Fact]
    public async Task Delete_Fails_WhenVehicleHasContracts()
    {
        var vehicleId = (await _service.CreateAsync(ValidInput())).Value;
        var customer = new Customer("Ana Teste", "ana@example.com", "910000001", "P-1");
        _database.Context.Customers.Add(customer);
        await _database.Context.SaveChangesAsync();
        _database.Context.RentalContracts.Add(
            new RentalContract(customer.Id, vehicleId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), 1000));
        await _database.Context.SaveChangesAsync();

        var result = await _service.DeleteAsync(vehicleId);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await _database.Context.Vehicles.CountAsync());
    }

    [Fact]
    public async Task GetAll_ShowsVehicleAsRented_OnlyWhileContractIsActive_EndDateInclusive()
    {
        var vehicleId = (await _service.CreateAsync(ValidInput())).Value;
        var customer = new Customer("Ana Teste", "ana@example.com", "910000001", "P-1");
        _database.Context.Customers.Add(customer);
        await _database.Context.SaveChangesAsync();
        _database.Context.RentalContracts.Add(
            new RentalContract(customer.Id, vehicleId, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 5), 1000));
        await _database.Context.SaveChangesAsync();

        // 2 October: first day of the contract
        Assert.True((await _service.GetAllAsync()).Single().IsRented);

        // 5 October: last day, still rented (end date is inclusive)
        _time.Advance(TimeSpan.FromDays(3));
        Assert.True((await _service.GetAllAsync()).Single().IsRented);

        // 6 October: contract is over
        _time.Advance(TimeSpan.FromDays(1));
        Assert.False((await _service.GetAllAsync()).Single().IsRented);
    }
}
using JapCarRental.Web.Controllers;
using JapCarRental.Web.Data;
using JapCarRental.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JapCarRental.Tests.Controllers;

public class ContractsControllerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly ContractsController _controller;

    public ContractsControllerTests()
    {
        var contractService = new ContractService(_database.Context, _time);
        var customerService = new CustomerService(_database.Context);
        _controller = new ContractsController(contractService, customerService, _time);
    }

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("2026-10-04", "2026-10-25")]
    [InlineData("04/10/2026", "25/10/2026")]
    [InlineData("04-10-2026", "25-10-2026")]
    public async Task AvailableVehicles_ReturnsAvailableVehicles_ForVariousDateFormats(string start, string end)
    {
        await DbSeeder.SeedAsync(_database.Context);

        var result = await _controller.AvailableVehicles(start, end);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var vehicles = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
        Assert.Equal(4, vehicles.Count());
    }

    [Theory]
    [InlineData(null, "2026-10-25")]
    [InlineData("2026-10-04", null)]
    [InlineData("invalid", "2026-10-25")]
    public async Task AvailableVehicles_ReturnsEmpty_ForInvalidDates(string? start, string? end)
    {
        await DbSeeder.SeedAsync(_database.Context);

        var result = await _controller.AvailableVehicles(start, end);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var vehicles = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
        Assert.Empty(vehicles);
    }
}

using JapCarRental.Web.Controllers;
using JapCarRental.Web.Data;
using JapCarRental.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;
using Xunit;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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
        _controller = new ContractsController(contractService, customerService, _time)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataProviderStub())
        };
    }

    private class TempDataProviderStub : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
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

    [Fact]
    public async Task Edit_Get_RedirectsWithTempDataError_WhenContractIsActiveOrFinished()
    {
        await DbSeeder.SeedAsync(_database.Context);
        // Contract 1 in DbSeeder is active (2026-10-01 to 2026-10-08, today is 2026-10-04)
        var activeContractId = _database.Context.RentalContracts.First(c => c.StartDate <= new DateOnly(2026, 10, 4) && c.EndDate >= new DateOnly(2026, 10, 4)).Id;

        var result = await _controller.Edit(activeContractId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Contratos em curso ou finalizados não podem ser alterados.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenContractIsUpcoming()
    {
        await DbSeeder.SeedAsync(_database.Context);
        // Contract 7 in DbSeeder is future/upcoming (2026-10-14 to 2026-10-18)
        var upcomingContractId = _database.Context.RentalContracts.First(c => c.StartDate > new DateOnly(2026, 10, 4)).Id;

        var result = await _controller.Edit(upcomingContractId);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", viewResult.ViewName);
    }
}

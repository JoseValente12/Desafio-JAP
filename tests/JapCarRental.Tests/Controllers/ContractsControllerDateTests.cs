using JapCarRental.Web.Controllers;
using JapCarRental.Web.Data;
using JapCarRental.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace JapCarRental.Tests.Controllers;

/// <summary>
/// Runs the date-dependent controller rules for several reference days.
/// The fake clock and the seed always use the same day, so the result must not depend on when the tests run.
/// </summary>
public class ContractsControllerDateTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    // Ordinary day, today's fixed date in the other tests, a month and year change, and a leap day.
    public static TheoryData<int, int, int> ReferenceDays => new()
    {
        { 2026, 10, 4 },
        { 2026, 10, 16 },
        { 2026, 11, 20 },
        { 2026, 12, 31 },
        { 2027, 1, 1 },
        { 2028, 2, 28 },
        { 2028, 2, 29 },
    };

    [Theory]
    [MemberData(nameof(ReferenceDays))]
    public async Task AvailableVehicles_Returns4_ForTheNext21Days_OnAnyReferenceDay(int year, int month, int day)
    {
        var today = new DateOnly(year, month, day);
        var controller = await CreateSeededControllerAsync(today);

        // Seed: 4 vehicles are rented today, 2 more are booked within the next 21 days, so 10 - 6 = 4 are free.
        var result = await controller.AvailableVehicles(
            today.ToString("yyyy-MM-dd"),
            today.AddDays(21).ToString("yyyy-MM-dd"));

        var json = Assert.IsType<JsonResult>(result);
        var vehicles = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
        Assert.Equal(4, vehicles.Count());
    }

    [Theory]
    [MemberData(nameof(ReferenceDays))]
    public async Task AvailableVehicles_ReturnsOnlyFreeVehicles_WhenTheIntervalIsInThePast(int year, int month, int day)
    {
        var today = new DateOnly(year, month, day);
        var controller = await CreateSeededControllerAsync(today);

        // 60 to 50 days ago nothing is rented in the seed, so all 10 vehicles are available.
        var result = await controller.AvailableVehicles(
            today.AddDays(-60).ToString("yyyy-MM-dd"),
            today.AddDays(-50).ToString("yyyy-MM-dd"));

        var json = Assert.IsType<JsonResult>(result);
        var vehicles = Assert.IsAssignableFrom<IEnumerable<object>>(json.Value);
        Assert.Equal(10, vehicles.Count());
    }

    [Theory]
    [MemberData(nameof(ReferenceDays))]
    public async Task Edit_Get_Redirects_WhenTheContractIsActiveToday(int year, int month, int day)
    {
        var today = new DateOnly(year, month, day);
        var controller = await CreateSeededControllerAsync(today);

        var activeContractId = _database.Context.RentalContracts
            .First(c => c.StartDate <= today && c.EndDate >= today).Id;

        var result = await controller.Edit(activeContractId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Contratos em curso ou finalizados não podem ser alterados.", controller.TempData["Error"]);
    }

    // The seed and the fake clock get the same day, which is what keeps the tests independent of the real date.
    private async Task<ContractsController> CreateSeededControllerAsync(DateOnly today)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(today.Year, today.Month, today.Day, 12, 0, 0, TimeSpan.Zero));

        await DbSeeder.SeedAsync(_database.Context, today);

        return new ContractsController(
            new ContractService(_database.Context, clock),
            new CustomerService(_database.Context),
            clock)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataProviderStub())
        };
    }

    private class TempDataProviderStub : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}

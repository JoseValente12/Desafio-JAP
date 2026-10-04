using JapCarRental.Web.Controllers;
using JapCarRental.Web.Models;
using JapCarRental.Web.Services;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Time.Testing;

namespace JapCarRental.Tests.Controllers;

public class VehiclesControllerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero));
    private readonly VehicleService _service;
    private readonly VehiclesController _controller;

    public VehiclesControllerTests()
    {
        // Real service and real (in-memory SQLite) database: the controller is tested end to end
        // without mocks, so a wrong query or a wrong mapping fails here.
        _service = new VehicleService(_database.Context, _time);
        _controller = new VehiclesController(_service);
    }

    public void Dispose() => _database.Dispose();

    private async Task AddVehicleAsync(string brand, string model, string plate) =>
        await _service.CreateAsync(new VehicleInput(brand, model, plate, 2021, FuelType.Petrol));

    // Adds vehicles with plates AA01BB, AA02BB, ... so every plate is unique.
    private async Task AddManyAsync(int count)
    {
        for (var i = 1; i <= count; i++)
            await AddVehicleAsync("Marca", $"Modelo{i}", $"AA{i:D2}BB");
    }

    private async Task<VehicleIndexViewModel> IndexAsync(string? search = null, int page = 1)
    {
        var result = await _controller.Index(search, page);
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<VehicleIndexViewModel>(view.Model);
    }

    // ---------- Paging ----------

    [Fact]
    public async Task Index_ShowsAtMostTenVehiclesPerPage()
    {
        await AddManyAsync(12);

        var model = await IndexAsync();

        Assert.Equal(10, model.Vehicles.Items.Count);
        Assert.Equal(12, model.Vehicles.TotalItems);
        Assert.Equal(2, model.Vehicles.TotalPages);
    }

    [Fact]
    public async Task Index_SecondPageHoldsTheRemainder()
    {
        await AddManyAsync(12);

        var model = await IndexAsync(page: 2);

        Assert.Equal(2, model.Vehicles.Items.Count);
    }

    [Fact]
    public async Task Index_ClampsAPageBeyondTheLastOne()
    {
        await AddManyAsync(12);

        var model = await IndexAsync(page: 999);

        Assert.Equal(2, model.Vehicles.Page);
    }

    // ---------- Search ----------

    [Fact]
    public async Task Index_SearchFindsByBrandAndModelTogether()
    {
        await AddVehicleAsync("Renault", "Clio", "AA11BB");
        await AddVehicleAsync("Peugeot", "208", "CC22DD");

        var model = await IndexAsync("renault clio");

        Assert.Equal("Clio", model.Vehicles.Items.Single().Model);
    }

    [Theory]
    [InlineData("aa11bb")]
    [InlineData("AA-11-BB")]   // the format shown in the list
    [InlineData("aa 11")]      // a partial plate with a space
    public async Task Index_SearchFindsAPlateIgnoringSeparatorsAndCase(string term)
    {
        await AddVehicleAsync("Renault", "Clio", "AA11BB");
        await AddVehicleAsync("Peugeot", "208", "CC22DD");

        var model = await IndexAsync(term);

        Assert.Equal("AA11BB", model.Vehicles.Items.Single().LicensePlate);
    }

    [Fact]
    public async Task Index_SearchWithNoMatch_ReturnsAnEmptyPage_ButKeepsTheFleetTotal()
    {
        await AddVehicleAsync("Renault", "Clio", "AA11BB");

        var model = await IndexAsync("tesla");

        Assert.Empty(model.Vehicles.Items);
        Assert.Equal(1, model.TotalVehicles); // lets the view tell "no results" from "no vehicles"
    }

    // ---------- Edit ----------

    [Fact]
    public async Task Edit_Post_ReturnsBadRequest_WhenTheFormIdDiffersFromTheRouteId()
    {
        await AddVehicleAsync("Renault", "Clio", "AA11BB");

        var result = await _controller.Edit(1, new VehicleFormViewModel { Id = 2 });

        Assert.IsType<BadRequestResult>(result);
    }

    // ---------- Business rules reach the form ----------

    [Fact]
    public async Task Create_Post_ShowsTheServiceErrorOnTheRightField_WhenThePlateAlreadyExists()
    {
        await AddVehicleAsync("Renault", "Clio", "AA11BB");
        var form = new VehicleFormViewModel
        {
            Brand = "Peugeot", Model = "208", LicensePlate = "aa-11-bb",
            ManufactureYear = 2022, FuelType = FuelType.Diesel
        };

        var result = await _controller.Create(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ContainsKey("LicensePlate"));
    }
}
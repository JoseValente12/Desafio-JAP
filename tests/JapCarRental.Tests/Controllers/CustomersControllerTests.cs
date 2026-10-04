using JapCarRental.Web.Controllers;
using JapCarRental.Web.Services;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JapCarRental.Tests.Controllers;

public class CustomersControllerTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly CustomerService _service;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        // NOTE: copy the exact constructor call from your CustomerServiceTests.
        // If CustomerService also takes a TimeProvider, pass a FakeTimeProvider here.
        _service = new CustomerService(_database.Context);
        _controller = new CustomersController(_service);
    }

    public void Dispose() => _database.Dispose();

    private async Task AddCustomerAsync(string name, string email, string phone = "910000001", string license = "P-1") =>
        await _service.CreateAsync(new CustomerInput(
            FullName: name, Email: email, PhoneNumber: phone, DrivingLicenseNumber: license));

    private async Task<CustomerIndexViewModel> IndexAsync(string? search = null, int page = 1)
    {
        var result = await _controller.Index(search, page);
        var view = Assert.IsType<ViewResult>(result);
        return Assert.IsType<CustomerIndexViewModel>(view.Model);
    }

    [Fact]
    public async Task Index_ShowsAtMostTenCustomersPerPage()
    {
        for (var i = 1; i <= 12; i++)
            await AddCustomerAsync($"Cliente {i}", $"cliente{i}@example.com", phone: $"9100000{i:D2}");

        var model = await IndexAsync();

        Assert.Equal(10, model.Customers.Items.Count);
        Assert.Equal(2, model.Customers.TotalPages);
    }

    [Theory]
    [InlineData("ana")]                  // part of the name, any case
    [InlineData("ANA@EXAMPLE")]          // part of the email
    [InlineData("911111111")]            // phone
    public async Task Index_SearchFindsByNameEmailOrPhone(string term)
    {
        await AddCustomerAsync("Ana Silva", "ana@example.com", phone: "911111111");
        await AddCustomerAsync("Rui Costa", "rui@example.com", phone: "922222222");

        var model = await IndexAsync(term);

        Assert.Equal("Ana Silva", model.Customers.Items.Single().FullName);
    }

    [Fact]
    public async Task Edit_Post_ReturnsBadRequest_WhenTheFormIdDiffersFromTheRouteId()
    {
        await AddCustomerAsync("Ana Silva", "ana@example.com");

        var result = await _controller.Edit(1, new CustomerFormViewModel { Id = 2 });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Create_Post_ShowsTheServiceErrorOnTheEmailField_WhenTheEmailAlreadyExists()
    {
        await AddCustomerAsync("Ana Silva", "ana@example.com");
        var form = new CustomerFormViewModel
        {
            FullName = "Outra Ana", Email = "ANA@example.com",
            PhoneNumber = "933333333", DrivingLicenseNumber = "P-2"
        };

        var result = await _controller.Create(form);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        Assert.True(_controller.ModelState.ContainsKey("Email"));
    }
}
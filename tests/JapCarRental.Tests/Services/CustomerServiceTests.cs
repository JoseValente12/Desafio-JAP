using JapCarRental.Web.Models;
using JapCarRental.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Tests.Services;

public class CustomerServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_database.Context);
    }

    public void Dispose() => _database.Dispose();

    private static CustomerInput ValidInput(string email = "ana@example.com") =>
        new("Ana Teste Silva", email, "910000001", "P-1000001");

    // Creates a vehicle and a contract for the given customer, so the customer cannot be deleted.
    private async Task AddContractForAsync(int customerId)
    {
        var vehicle = new Vehicle("Renault", "Clio", "AA11BB", 2021, FuelType.Petrol);
        _database.Context.Vehicles.Add(vehicle);
        await _database.Context.SaveChangesAsync();

        _database.Context.RentalContracts.Add(
            new RentalContract(customerId, vehicle.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5), 1000));
        await _database.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_Succeeds_AndNormalizesTheData()
    {
        var input = new CustomerInput("Ana Teste Silva", "  Ana@Example.COM ", "910 000-001", " p-123 ");

        var result = await _service.CreateAsync(input);

        Assert.True(result.Succeeded);
        var saved = await _database.Context.Customers.SingleAsync();
        Assert.Equal("ana@example.com", saved.Email);
        Assert.Equal("910000001", saved.PhoneNumber);
        Assert.Equal("P-123", saved.DrivingLicenseNumber);
    }

    [Fact]
    public async Task Create_Fails_WhenEmailAlreadyExists_IgnoringCase()
    {
        await _service.CreateAsync(ValidInput("ana@example.com"));

        var result = await _service.CreateAsync(ValidInput("ANA@Example.com"));

        Assert.False(result.Succeeded);
        Assert.Contains("Email", result.Errors.Keys);
        Assert.Equal(1, await _database.Context.Customers.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("ana.example.com")]
    [InlineData("ana@example")]
    [InlineData("Ana <ana@example.com>")]
    public async Task Create_Fails_WhenEmailIsInvalid(string email)
    {
        var result = await _service.CreateAsync(ValidInput(email));

        Assert.False(result.Succeeded);
        Assert.Contains("Email", result.Errors.Keys);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefghi")]
    public async Task Create_Fails_WhenPhoneIsInvalid(string phone)
    {
        var input = ValidInput() with { PhoneNumber = phone };

        var result = await _service.CreateAsync(input);

        Assert.False(result.Succeeded);
        Assert.Contains("PhoneNumber", result.Errors.Keys);
    }

    [Fact]
    public async Task Update_Succeeds_WhenKeepingTheOwnEmail()
    {
        var created = await _service.CreateAsync(ValidInput("ana@example.com"));

        var result = await _service.UpdateAsync(created.Value, ValidInput("ana@example.com") with { FullName = "Ana Maria Silva" });

        Assert.True(result.Succeeded);
        var saved = await _database.Context.Customers.SingleAsync();
        Assert.Equal("Ana Maria Silva", saved.FullName);
    }

    [Fact]
    public async Task Update_Fails_WhenEmailBelongsToAnotherCustomer()
    {
        await _service.CreateAsync(ValidInput("ana@example.com"));
        var second = await _service.CreateAsync(ValidInput("bruno@example.com"));

        var result = await _service.UpdateAsync(second.Value, ValidInput("ana@example.com"));

        Assert.False(result.Succeeded);
        Assert.Contains("Email", result.Errors.Keys);
    }

    [Fact]
    public async Task Delete_Fails_WhenCustomerHasContracts()
    {
        var customerId = (await _service.CreateAsync(ValidInput())).Value;
        await AddContractForAsync(customerId);

        var result = await _service.DeleteAsync(customerId);

        Assert.False(result.Succeeded);
        Assert.Equal(1, await _database.Context.Customers.CountAsync());
    }

    [Fact]
    public async Task Delete_Succeeds_WhenCustomerHasNoContracts()
    {
        var customerId = (await _service.CreateAsync(ValidInput())).Value;

        var result = await _service.DeleteAsync(customerId);

        Assert.True(result.Succeeded);
        Assert.Equal(0, await _database.Context.Customers.CountAsync());
    }

    [Fact]
    public async Task GetAll_ReturnsTheNumberOfContractsOfEachCustomer()
    {
        var withContract = (await _service.CreateAsync(ValidInput("ana@example.com"))).Value;
        await _service.CreateAsync(ValidInput("bruno@example.com"));
        await AddContractForAsync(withContract);

        var list = await _service.GetAllAsync();

        Assert.Equal(1, list.Single(c => c.Email == "ana@example.com").ContractCount);
        Assert.Equal(0, list.Single(c => c.Email == "bruno@example.com").ContractCount);
    }
}
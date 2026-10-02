using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerListItem>> GetAllAsync();

    Task<Customer?> GetByIdAsync(int id);

    /// <summary>Creates a customer. Fails on invalid data or duplicate email.</summary>
    Task<OperationResult<int>> CreateAsync(CustomerInput input);

    /// <summary>Updates a customer. Fails if not found or if the email belongs to another customer.</summary>
    Task<OperationResult> UpdateAsync(int id, CustomerInput input);

    /// <summary>Deletes a customer. Fails if the customer has rental contracts.</summary>
    Task<OperationResult> DeleteAsync(int id);
}
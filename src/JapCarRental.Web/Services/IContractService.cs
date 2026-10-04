namespace JapCarRental.Web.Services;

public interface IContractService
{
    /// <summary>Lists all contracts, newest first, with customer and vehicle data.</summary>
    Task<IReadOnlyList<ContractListItem>> GetAllAsync();

    Task<ContractListItem?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a contract. Fails on invalid dates, unknown customer or vehicle,
    /// or when the vehicle is already rented in part of the period.
    /// </summary>
    Task<OperationResult<int>> CreateAsync(ContractInput input);

    /// <summary>
    /// Updates a contract that has not started yet.
    /// Fails if contract already started, finished, or cancelled, or on overlap.
    /// </summary>
    Task<OperationResult> UpdateAsync(int id, ContractInput input);

    /// <summary>Cancels (deletes) a contract that has not started yet.</summary>
    Task<OperationResult> CancelAsync(int id);

    /// <summary>Lists the vehicles with no contract overlapping the given period.</summary>
    Task<IReadOnlyList<VehicleOption>> GetAvailableVehiclesAsync(DateOnly startDate, DateOnly endDate, int? ignoreContractId = null);
}
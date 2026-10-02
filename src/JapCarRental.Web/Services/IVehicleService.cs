using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

public interface IVehicleService
{
    /// <summary>Lists all vehicles with their status for today.</summary>
    Task<IReadOnlyList<VehicleListItem>> GetAllAsync();

    Task<Vehicle?> GetByIdAsync(int id);

    /// <summary>Creates a vehicle. Fails on invalid data or duplicate license plate.</summary>
    Task<OperationResult<int>> CreateAsync(VehicleInput input);

    /// <summary>Updates a vehicle. Fails if not found or if the license plate belongs to another vehicle.</summary>
    Task<OperationResult> UpdateAsync(int id, VehicleInput input);

    /// <summary>Deletes a vehicle. Fails if it has rental contracts.</summary>
    Task<OperationResult> DeleteAsync(int id);
}
namespace JapCarRental.Web.Services;

/// <summary>
/// Read model for contract pages: includes customer and vehicle data
/// so the pages do not need extra queries.
/// </summary>
public record ContractListItem(
    int Id,
    int CustomerId,
    string CustomerName,
    int VehicleId,
    string VehicleName,
    string LicensePlate,
    DateOnly StartDate,
    DateOnly EndDate,
    int InitialMileage,
    DateOnly? CancelledOn,
    ContractStatus Status);
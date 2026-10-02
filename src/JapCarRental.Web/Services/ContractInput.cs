namespace JapCarRental.Web.Services;

/// <summary>Data needed to create a rental contract.</summary>
public record ContractInput(
    int CustomerId,
    int VehicleId,
    DateOnly StartDate,
    DateOnly EndDate,
    int InitialMileage);
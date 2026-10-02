using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

/// <summary>
/// Data needed to create or edit a vehicle. Keeps the services independent
/// from the MVC view models.
/// </summary>
public record VehicleInput(
    string Brand,
    string Model,
    string LicensePlate,
    int ManufactureYear,
    FuelType FuelType);
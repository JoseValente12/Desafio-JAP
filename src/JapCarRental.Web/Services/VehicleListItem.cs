using JapCarRental.Web.Models;

namespace JapCarRental.Web.Services;

/// <summary>
/// Read model for the vehicle list. IsRented is calculated from the contracts
/// for a given day and is never stored in the database.
/// </summary>
public record VehicleListItem(
    int Id,
    string Brand,
    string Model,
    string LicensePlate,
    int ManufactureYear,
    FuelType FuelType,
    bool IsRented);
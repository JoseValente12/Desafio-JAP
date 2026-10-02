namespace JapCarRental.Web.Services;

/// <summary>
/// Data needed to create or edit a customer. Keeps the services independent
/// from the MVC view models.
/// </summary>
public record CustomerInput(
    string FullName,
    string Email,
    string PhoneNumber,
    string DrivingLicenseNumber);
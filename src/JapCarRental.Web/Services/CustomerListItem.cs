namespace JapCarRental.Web.Services;

/// <summary>
/// Read model for the customer list. ContractCount lets the page tell
/// whether the customer can be deleted.
/// </summary>
public record CustomerListItem(
    int Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string DrivingLicenseNumber,
    int ContractCount);
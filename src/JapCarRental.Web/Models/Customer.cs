namespace JapCarRental.Web.Models;

public class Customer
{
    private readonly List<RentalContract> _contracts = new();

    private Customer() { }

    public Customer(string fullName, string email, string phoneNumber, string drivingLicenseNumber)
    {
        UpdateDetails(fullName, email, phoneNumber, drivingLicenseNumber);
    }

    public int Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string DrivingLicenseNumber { get; private set; } = string.Empty;

    public IReadOnlyCollection<RentalContract> Contracts => _contracts;

    public void UpdateDetails(string fullName, string email, string phoneNumber, string drivingLicenseNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(drivingLicenseNumber);

        FullName = fullName.Trim();
        Email = email.Trim();
        PhoneNumber = phoneNumber.Trim();
        DrivingLicenseNumber = drivingLicenseNumber.Trim();
    }
}
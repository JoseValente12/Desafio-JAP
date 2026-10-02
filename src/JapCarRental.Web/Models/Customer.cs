namespace JapCarRental.Web.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DrivingLicenseNumber { get; set; } = string.Empty;

    public ICollection<RentalContract> Contracts { get; set; } = new List<RentalContract>();
}
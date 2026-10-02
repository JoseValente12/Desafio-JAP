namespace JapCarRental.Web.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public int ManufactureYear { get; set; }
    public FuelType FuelType { get; set; }

    public ICollection<RentalContract> Contracts { get; set; } = new List<RentalContract>();
}
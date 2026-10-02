namespace JapCarRental.Web.Models;

public class Vehicle
{
    private readonly List<RentalContract> _contracts = new();

    private Vehicle() { }

    public Vehicle(string brand, string model, string licensePlate, int manufactureYear, FuelType fuelType)
    {
        Update(brand, model, licensePlate, manufactureYear, fuelType);
    }

    public int Id { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string LicensePlate { get; private set; } = string.Empty;
    public int ManufactureYear { get; private set; }
    public FuelType FuelType { get; private set; }

    public IReadOnlyCollection<RentalContract> Contracts => _contracts;

    public void Update(string brand, string model, string licensePlate, int manufactureYear, FuelType fuelType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);

        if (manufactureYear < 1900 || manufactureYear > DateTime.Today.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(manufactureYear), $"The year of manufacture must be between 1900 and {DateTime.Today.Year}.");
        }

        if (!Enum.IsDefined(fuelType))
        {
            throw new ArgumentOutOfRangeException(nameof(fuelType), "Invalid fuel type.");
        }

        Brand = brand.Trim();
        Model = model.Trim();
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        ManufactureYear = manufactureYear;
        FuelType = fuelType;
    }
}
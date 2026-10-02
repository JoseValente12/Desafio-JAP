namespace JapCarRental.Web.Models;

public class RentalContract
{
    // Required by EF Core
    private RentalContract() { }

    public RentalContract(int customerId, int vehicleId, DateOnly startDate, DateOnly endDate, int initialMileage)
    {
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (vehicleId <= 0) throw new ArgumentOutOfRangeException(nameof(vehicleId));

        CustomerId = customerId;
        VehicleId = vehicleId;
        UpdateDatesAndMileage(startDate, endDate, initialMileage);
    }

    public int Id { get; private set; }

    public int CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    public int VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int InitialMileage { get; private set; }

    public void UpdateDatesAndMileage(DateOnly startDate, DateOnly endDate, int initialMileage)
    {
        // Rule from the challenge: end date must be after the start date
        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.", nameof(endDate));
        }

        if (initialMileage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialMileage), "Initial mileage cannot be negative.");
        }

        StartDate = startDate;
        EndDate = endDate;
        InitialMileage = initialMileage;
    }

    public bool IsActiveOn(DateOnly date) => date >= StartDate && date <= EndDate;
}
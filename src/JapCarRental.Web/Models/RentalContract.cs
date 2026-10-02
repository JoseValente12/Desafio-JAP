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
    
    // Date on which the contract was cancelled. Null means it was never cancelled.
    // We store the date instead of a bool so the history shows WHEN it happened,
    // and the row is kept (soft cancellation) instead of being deleted.
    public DateOnly? CancelledOn { get; private set; }

    // Derived from CancelledOn so the two can never disagree.
    public bool IsCancelled => CancelledOn.HasValue;

    // A cancelled contract is never active, whatever its dates say.
    public bool IsActiveOn(DateOnly date) =>
        !IsCancelled && date >= StartDate && date <= EndDate;

    // The entity only guards its own invariant (cannot cancel twice).
    // "Only before it starts" depends on today's date, so that rule lives in ContractService.
    public void Cancel(DateOnly today)
    {
        if (IsCancelled)
            throw new InvalidOperationException("Contract is already cancelled.");

        CancelledOn = today;
    }
}
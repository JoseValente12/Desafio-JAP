namespace JapCarRental.Web.Services;

/// <summary>Whether a vehicle is out on a contract today. Never stored: it comes from the contracts.</summary>
public enum VehicleAvailability
{
    Available,
    Rented
}

/// <summary>
/// The filters of the vehicle list: free text and availability today.
/// A pure function over the list, so every rule can be unit tested without a database.
/// </summary>
public record VehicleFilter(string? Search = null, VehicleAvailability? Availability = null)
{
    public IEnumerable<VehicleListItem> Apply(IEnumerable<VehicleListItem> vehicles)
    {
        var result = vehicles;

        var term = Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // Plates are stored without separators (AA11BB), so "aa-11" is compared as "aa11".
            var plateTerm = new string(term.Where(char.IsLetterOrDigit).ToArray());

            result = result.Where(v =>
                Matches(v.Brand, term) ||
                Matches(v.Model, term) ||
                (plateTerm.Length > 0 && Matches(v.LicensePlate, plateTerm)));
        }

        if (Availability is { } availability)
            result = result.Where(v => v.IsRented == (availability == VehicleAvailability.Rented));

        return result;
    }

    // Case-insensitive "contains".
    private static bool Matches(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
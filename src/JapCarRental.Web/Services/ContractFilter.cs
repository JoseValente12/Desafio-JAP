namespace JapCarRental.Web.Services;

/// <summary>
/// The filters of the contract list: free text, status and a period of dates.
/// A pure function over the list, so every rule can be unit tested without a database.
/// </summary>
public record ContractFilter(
    string? Search = null,
    ContractStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null)
{
    public IEnumerable<ContractListItem> Apply(IEnumerable<ContractListItem> contracts)
    {
        var result = contracts;

        var term = Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            // Plates are stored without separators (AA11BB), so "aa-11" is compared as "aa11".
            var plateTerm = new string(term.Where(char.IsLetterOrDigit).ToArray());

            result = result.Where(c =>
                Matches(c.CustomerName, term) ||
                Matches(c.VehicleName, term) ||
                (plateTerm.Length > 0 && Matches(c.LicensePlate, plateTerm)));
        }

        if (Status is { } status)
            result = result.Where(c => c.Status == status);

        // If the dates were typed the wrong way round, swap them instead of showing an empty list.
        var (from, to) = From is { } f && To is { } t && f > t ? (To, From) : (From, To);

        // A contract belongs to the period if it shares at least one day with it
        // (the same overlap rule used to create contracts; both ends are inclusive).
        if (from is { } start)
            result = result.Where(c => c.EndDate >= start);
        if (to is { } end)
            result = result.Where(c => c.StartDate <= end);

        return result;
    }

    // Case-insensitive "contains".
    private static bool Matches(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
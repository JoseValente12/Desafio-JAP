namespace JapCarRental.Web.Services;

public interface IDashboardService
{
    /// <summary>Builds the figures for the home page, as of today.</summary>
    Task<DashboardSummary> GetSummaryAsync();
}
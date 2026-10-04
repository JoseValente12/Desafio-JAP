using JapCarRental.Web.Services;

namespace JapCarRental.Web.ViewModels;

public record ContractIndexViewModel(
    PagedList<ContractListItem> Contracts,
    string? Search,
    int TotalContracts,
    ContractStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null);
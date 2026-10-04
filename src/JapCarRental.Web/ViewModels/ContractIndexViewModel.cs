using JapCarRental.Web.Services;

namespace JapCarRental.Web.ViewModels;

public record ContractIndexViewModel(
    PagedList<ContractListItem> Contracts,
    string? Search,
    int TotalContracts);   // all contracts, ignoring the search, to tell "empty" from "no results"
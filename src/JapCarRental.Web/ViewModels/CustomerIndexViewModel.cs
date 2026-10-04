using JapCarRental.Web.Services;

namespace JapCarRental.Web.ViewModels;

public record CustomerIndexViewModel(
    PagedList<CustomerListItem> Customers,
    string? Search,
    int TotalCustomers);   // all customers, ignoring the search, to tell "empty" from "no results"
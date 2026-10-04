using JapCarRental.Web.Services;

namespace JapCarRental.Web.ViewModels;

public record VehicleIndexViewModel(
    PagedList<VehicleListItem> Vehicles,
    string? Search,
    int TotalVehicles,     // whole fleet, ignoring the search, to tell "empty" from "no results"
    int RentedVehicles);   // rented today across the whole fleet, shown in the page header
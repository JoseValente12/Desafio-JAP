using System.Globalization;
using JapCarRental.Web.Extensions;
using JapCarRental.Web.Services;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JapCarRental.Web.Controllers;

public class ContractsController : Controller
{
    private const int PageSize = 10;

    private readonly IContractService _contracts;
    private readonly ICustomerService _customers;
    private readonly TimeProvider _timeProvider;

    public ContractsController(
        IContractService contracts,
        ICustomerService customers,
        TimeProvider timeProvider)
    {
        _contracts = contracts;
        _customers = customers;
        _timeProvider = timeProvider;
    }

    // GET so a search can be bookmarked and the browser back button works.
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var all = await _contracts.GetAllAsync();
        var term = search?.Trim();

        IEnumerable<ContractListItem> filtered = all;
        if (!string.IsNullOrEmpty(term))
        {
            // Plates are stored without separators (AA11BB), so "aa-11" is compared as "aa11".
            var plateTerm = new string(term.Where(char.IsLetterOrDigit).ToArray());

            filtered = all.Where(c =>
                Matches(c.CustomerName, term) ||
                Matches(c.VehicleName, term) ||
                (plateTerm.Length > 0 && Matches(c.LicensePlate, plateTerm)));
        }

        return View(new ContractIndexViewModel(
            PagedList<ContractListItem>.Create(filtered, page, PageSize),
            term,
            all.Count));
    }

    // Case-insensitive "contains".
    private static bool Matches(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> Create()
    {
        // Sensible defaults (today to tomorrow) so the vehicle dropdown starts with real options.
        var today = Today();
        var form = new ContractFormViewModel { StartDate = today, EndDate = today.AddDays(1) };

        await LoadOptionsAsync(form);
        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var result = await _contracts.CreateAsync(ToInput(form));
            if (result.Succeeded)
            {
                TempData["Success"] = "Contrato criado com sucesso.";
                return RedirectToAction(nameof(Index));
            }

            result.AddToModelState(ModelState);
        }

        // Any failure shows the form again, with fresh dropdowns for the submitted dates.
        await LoadOptionsAsync(form);
        return View(form);
    }

    // JSON used by available-vehicles.js: only the vehicles that are free in the period.
    [HttpGet]
    public async Task<IActionResult> AvailableVehicles(string? start, string? end)
    {
        if (!TryParseDate(start, out var startDate) || !TryParseDate(end, out var endDate))
        {
            return Json(Array.Empty<object>());
        }

        var vehicles = await _contracts.GetAvailableVehiclesAsync(startDate, endDate);
        return Json(vehicles.Select(v => new { id = v.Id, description = v.Description }));
    }

    private static bool TryParseDate(string? input, out DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            date = default;
            return false;
        }

        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy/MM/dd"];
        return DateOnly.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
            || DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    // Contracts are never deleted: cancelling keeps them in the history.
    // The service only allows it before the contract starts.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _contracts.CancelAsync(id);

        if (result.Succeeded)
            TempData["Success"] = "Contrato cancelado.";
        else
            TempData["Error"] = string.Join(" ", result.Errors.SelectMany(e => e.Value));

        return RedirectToAction(nameof(Index));
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private async Task LoadOptionsAsync(ContractFormViewModel form)
    {
        var customers = await _customers.GetAllAsync();
        form.Customers = customers
            .Select(c => new SelectListItem($"{c.FullName} ({c.Email})", c.Id.ToString()))
            .ToList();

        // Only vehicles free in the chosen period; empty until both dates are valid.
        if (form.StartDate is { } start && form.EndDate is { } end)
        {
            var vehicles = await _contracts.GetAvailableVehiclesAsync(start, end);
            form.Vehicles = vehicles
                .Select(v => new SelectListItem(v.Description, v.Id.ToString()))
                .ToList();
        }
    }

    // Named arguments so the mapping does not depend on the record's parameter order.
    // The values are safe to read: [Required] has already passed when this runs.
    private static ContractInput ToInput(ContractFormViewModel f) =>
        new(CustomerId: f.CustomerId!.Value,
            VehicleId: f.VehicleId!.Value,
            StartDate: f.StartDate!.Value,
            EndDate: f.EndDate!.Value,
            InitialMileage: f.InitialMileage);
}
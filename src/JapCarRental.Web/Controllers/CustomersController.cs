using JapCarRental.Web.Extensions;
using JapCarRental.Web.Services;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JapCarRental.Web.Controllers;

public class CustomersController : Controller
{
    private const int PageSize = 10;

    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    // GET so a search can be bookmarked and the browser back button works.
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var all = await _customers.GetAllAsync();
        var term = search?.Trim();

        IEnumerable<CustomerListItem> filtered = all;
        if (!string.IsNullOrEmpty(term))
        {
            filtered = all.Where(c =>
                Matches(c.FullName, term) ||
                Matches(c.Email, term) ||
                Matches(c.PhoneNumber, term) ||
                Matches(c.DrivingLicenseNumber, term));
        }

        return View(new CustomerIndexViewModel(
            PagedList<CustomerListItem>.Create(filtered, page, PageSize),
            term,
            all.Count));
    }

    // Case-insensitive "contains" so searching "ana" finds "Ana", "ANA" and "Mariana".
    private static bool Matches(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

private static bool Contains(string value, string term) =>
    value.Contains(term, StringComparison.OrdinalIgnoreCase);
    public IActionResult Create() => View("Form", new CustomerFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        var result = await _customers.CreateAsync(ToInput(form));
        if (!result.Succeeded)
        {
            result.AddToModelState(ModelState);
            return View("Form", form);
        }

        TempData["Success"] = "Cliente criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        if (customer is null) return NotFound();

        return View("Form", new CustomerFormViewModel
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            DrivingLicenseNumber = customer.DrivingLicenseNumber
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel form)
    {
        // The route id is the source of truth. A different hidden Id means the request was tampered with.
        if (id != form.Id) return BadRequest();

        if (!ModelState.IsValid) return View("Form", form);

        var result = await _customers.UpdateAsync(id, ToInput(form));
        if (!result.Succeeded)
        {
            result.AddToModelState(ModelState);
            return View("Form", form);
        }

        TempData["Success"] = "Cliente atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // Changes data, so it is a POST. The service refuses customers that have contracts.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customers.DeleteAsync(id);

        if (result.Succeeded)
            TempData["Success"] = "Cliente removido.";
        else
            TempData["Error"] = string.Join(" ", result.Errors.SelectMany(e => e.Value));

        return RedirectToAction(nameof(Index));
    }

    // Named arguments so the mapping does not depend on the record's parameter order.
    private static CustomerInput ToInput(CustomerFormViewModel f) =>
        new(FullName: f.FullName,
            Email: f.Email,
            PhoneNumber: f.PhoneNumber,
            DrivingLicenseNumber: f.DrivingLicenseNumber);
}
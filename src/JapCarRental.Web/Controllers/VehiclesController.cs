using JapCarRental.Web.Extensions;
using JapCarRental.Web.Services;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JapCarRental.Web.Controllers;

public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicles;

    public VehiclesController(IVehicleService vehicles)
    {
        _vehicles = vehicles;
    }

    public async Task<IActionResult> Index() =>
        View(await _vehicles.GetAllAsync());

    public IActionResult Create() => View("Form", new VehicleFormViewModel());

    // ValidateAntiForgeryToken protects every POST against cross-site request forgery.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VehicleFormViewModel form)
    {
        // Fast annotation checks first; the service then applies the business rules.
        if (!ModelState.IsValid) return View("Form", form);

        var result = await _vehicles.CreateAsync(ToInput(form));
        if (!result.Succeeded)
        {
            result.AddToModelState(ModelState);
            return View("Form", form);
        }

        TempData["Success"] = "Veículo criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await _vehicles.GetByIdAsync(id);
        if (vehicle is null) return NotFound();

        return View("Form", new VehicleFormViewModel
        {
            Id = vehicle.Id,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            LicensePlate = vehicle.LicensePlate,
            ManufactureYear = vehicle.ManufactureYear,
            FuelType = vehicle.FuelType
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VehicleFormViewModel form)
    {
        if (!ModelState.IsValid) return View("Form", form);

        // The id comes from the route, not from the form, so a tampered hidden field changes nothing.
        var result = await _vehicles.UpdateAsync(id, ToInput(form));
        if (!result.Succeeded)
        {
            result.AddToModelState(ModelState);
            return View("Form", form);
        }

        TempData["Success"] = "Veículo atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    // Deleting changes data, so it is a POST (never a GET link that a crawler could follow).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _vehicles.DeleteAsync(id);

        if (result.Succeeded)
            TempData["Success"] = "Veículo removido.";
        else
            TempData["Error"] = string.Join(" ", result.Errors.SelectMany(e => e.Value));

        return RedirectToAction(nameof(Index));
    }

    private static VehicleInput ToInput(VehicleFormViewModel f) =>
        new(f.Brand, f.Model, f.LicensePlate, f.ManufactureYear, f.FuelType);
}
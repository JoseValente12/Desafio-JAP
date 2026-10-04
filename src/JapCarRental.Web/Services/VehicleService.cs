using System.Text.RegularExpressions;
using JapCarRental.Web.Data;
using JapCarRental.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Web.Services;

public class VehicleService : IVehicleService
{
    private static readonly Regex PlateFormat = new("^[A-Z0-9]{6}$");

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public VehicleService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<VehicleListItem>> GetAllAsync()
    {
        var today = Today();

        // The status is calculated in the query, never stored. The date comparison is written
        // out (instead of calling RentalContract.IsActiveOn) because EF cannot translate methods to SQL.
        return await _db.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .Select(v => new VehicleListItem(
                v.Id,
                v.Brand,
                v.Model,
                v.LicensePlate,
                v.ManufactureYear,
                v.FuelType,
                v.Contracts.Any(c => c.CancelledOn == null
                                 && c.StartDate <= today
                                 && c.EndDate >= today)))
            .ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(int id) => await _db.Vehicles.FindAsync(id);

    public async Task<OperationResult<int>> CreateAsync(VehicleInput input)
    {
        var validation = ValidateInput(input);
        var plate = NormalizePlate(input.LicensePlate);

        if (validation.Succeeded && await PlateExistsAsync(plate, ignoreId: null))
        {
            validation.AddError(nameof(VehicleInput.LicensePlate), "Já existe um veículo com esta matrícula.");
        }

        if (!validation.Succeeded)
        {
            return Fail<int>(validation);
        }

        var vehicle = new Vehicle(input.Brand, input.Model, plate, input.ManufactureYear, input.FuelType);
        _db.Vehicles.Add(vehicle);

        try
        {
            // The unique index on LicensePlate is the last guard against two simultaneous requests.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(vehicle).State = EntityState.Detached;

            var plateTaken = await _db.Vehicles.AnyAsync(v => v.LicensePlate == vehicle.LicensePlate);
            if (!plateTaken) throw;

            var result = new OperationResult<int>();
            result.AddError(nameof(VehicleInput.LicensePlate), "Já existe um veículo com esta matrícula.");
            return result;
        }

        return OperationResult<int>.Success(vehicle.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, VehicleInput input)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null)
        {
            return OperationResult.Failure("Veículo não encontrado.");
        }

        var validation = ValidateInput(input);
        var plate = NormalizePlate(input.LicensePlate);

        // The vehicle being edited may keep its own plate, so it is ignored in the check.
        if (validation.Succeeded && await PlateExistsAsync(plate, ignoreId: id))
        {
            validation.AddError(nameof(VehicleInput.LicensePlate), "Já existe outro veículo com esta matrícula.");
        }

        if (!validation.Succeeded)
        {
            return validation;
        }

        vehicle.Update(input.Brand, input.Model, plate, input.ManufactureYear, input.FuelType);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(vehicle).State = EntityState.Unchanged;

            var plateTaken = await _db.Vehicles.AnyAsync(v => v.Id != id && v.LicensePlate == vehicle.LicensePlate);
            if (!plateTaken) throw;

            var result = new OperationResult();
            result.AddError(nameof(VehicleInput.LicensePlate), "Já existe outro veículo com esta matrícula.");
            return result;
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var vehicle = await _db.Vehicles.FindAsync(id);
        if (vehicle is null)
        {
            return OperationResult.Failure("Veículo não encontrado.");
        }

        // Business rule: the history of contracts must be kept (the FK is also Restrict).
        if (await _db.RentalContracts.AnyAsync(c => c.VehicleId == id))
        {
            return OperationResult.Failure("Não é possível apagar um veículo que tem contratos.");
        }

        _db.Vehicles.Remove(vehicle);
        await _db.SaveChangesAsync();

        return OperationResult.Success();
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    // Checks that do not need the database. Lengths match the Fluent API configuration.
    // Done here so users get field messages instead of the entity's exceptions.
    private OperationResult ValidateInput(VehicleInput input)
    {
        var result = new OperationResult();

        if (string.IsNullOrWhiteSpace(input.Brand))
            result.AddError(nameof(VehicleInput.Brand), "A marca é obrigatória.");
        else if (input.Brand.Trim().Length > 50)
            result.AddError(nameof(VehicleInput.Brand), "A marca não pode ter mais de 50 caracteres.");

        if (string.IsNullOrWhiteSpace(input.Model))
            result.AddError(nameof(VehicleInput.Model), "O modelo é obrigatório.");
        else if (input.Model.Trim().Length > 50)
            result.AddError(nameof(VehicleInput.Model), "O modelo não pode ter mais de 50 caracteres.");

        if (string.IsNullOrWhiteSpace(input.LicensePlate))
            result.AddError(nameof(VehicleInput.LicensePlate), "A matrícula é obrigatória.");
        else if (!PlateFormat.IsMatch(NormalizePlate(input.LicensePlate)))
            result.AddError(nameof(VehicleInput.LicensePlate), "A matrícula deve ter 6 letras ou números, por exemplo AA33BD.");

        var currentYear = Today().Year;
        if (input.ManufactureYear < 1900 || input.ManufactureYear > currentYear)
            result.AddError(nameof(VehicleInput.ManufactureYear), $"O ano de fabrico deve estar entre 1900 e {currentYear}.");

        if (!Enum.IsDefined(input.FuelType))
            result.AddError(nameof(VehicleInput.FuelType), "Selecione um tipo de combustível válido.");

        return result;
    }

    // "aa33bd", "AA 33 BD" and "aa-33-bd" all become "AA33BD".
    private static string NormalizePlate(string? raw) =>
        new string((raw ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private Task<bool> PlateExistsAsync(string plate, int? ignoreId)
    {
    // When creating there is no vehicle to ignore. When editing, the vehicle being
    // edited may keep its own plate, so its Id is excluded from the check.
        return _db.Vehicles.AnyAsync(v =>
            v.LicensePlate == plate &&
            (!ignoreId.HasValue || v.Id != ignoreId.Value));
    }

    // Copies the errors of a non-generic result into a typed one.
    private static OperationResult<T> Fail<T>(OperationResult source)
    {
        var result = new OperationResult<T>();

        foreach (var (field, messages) in source.Errors)
        {
            foreach (var message in messages)
            {
                result.AddError(field, message);
            }
        }

        return result;
    }
}
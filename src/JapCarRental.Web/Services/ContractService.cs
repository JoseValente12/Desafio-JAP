using System.Globalization;
using JapCarRental.Web.Data;
using JapCarRental.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Web.Services;

public class ContractService : IContractService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public ContractService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ContractListItem>> GetAllAsync() =>
        await QueryAsync(_db.RentalContracts);

    public async Task<ContractListItem?> GetByIdAsync(int id) =>
        (await QueryAsync(_db.RentalContracts.Where(c => c.Id == id))).SingleOrDefault();

    public async Task<OperationResult<int>> CreateAsync(ContractInput input)
    {
        var validation = ValidateInput(input, Today());

        // Only hit the database when the basic checks pass.
        if (validation.Succeeded)
        {
            await ValidateAgainstDatabaseAsync(input, validation);
        }

        if (!validation.Succeeded)
        {
            return Fail<int>(validation);
        }

        var contract = new RentalContract(
            input.CustomerId,
            input.VehicleId,
            input.StartDate,
            input.EndDate,
            input.InitialMileage);

        _db.RentalContracts.Add(contract);

        // Known limitation: two requests for the same vehicle at the same instant could both pass
        // the overlap check. SQL Server has no constraint for overlapping ranges; a serializable
        // transaction or a lock per vehicle would close this gap if needed.
        await _db.SaveChangesAsync();

        return OperationResult<int>.Success(contract.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, ContractInput input)
    {
        var contract = await _db.RentalContracts.FindAsync(id);
        if (contract is null)
        {
            return OperationResult.Failure("Contrato não encontrado.");
        }

        var today = Today();

        // Business rule: contracts already started (or finished or cancelled) cannot be edited.
        if (contract.StartDate <= today || contract.IsCancelled)
        {
            return OperationResult.Failure("Contratos em curso ou finalizados não podem ser alterados.");
        }

        var validation = ValidateInput(input, today);
        if (validation.Succeeded)
        {
            await ValidateAgainstDatabaseAsync(input, validation, ignoreContractId: id);
        }

        if (!validation.Succeeded)
        {
            return validation;
        }

        contract.Update(input.CustomerId, input.VehicleId, input.StartDate, input.EndDate, input.InitialMileage);
        await _db.SaveChangesAsync();

        return OperationResult.Success();
    }

    // CHANGED: contracts are never deleted. Cancelling keeps the row so the history stays complete.
    public async Task<OperationResult> CancelAsync(int id)
    {
        var contract = await _db.RentalContracts.FindAsync(id);
        if (contract is null)
        {
            return OperationResult.Failure("Contrato não encontrado.");
        }

        if (contract.IsCancelled)
        {
            return OperationResult.Failure("Este contrato já foi cancelado.");
        }

        // Business rule: contracts already started (or finished) are part of the history and cannot be cancelled.
        var today = Today();
        if (contract.StartDate <= today)
        {
            return OperationResult.Failure("Só é possível cancelar contratos que ainda não começaram.");
        }

        contract.Cancel(today);
        await _db.SaveChangesAsync();

        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<VehicleOption>> GetAvailableVehiclesAsync(DateOnly startDate, DateOnly endDate, int? ignoreContractId = null)
    {
        if (endDate < startDate)
        {
            return Array.Empty<VehicleOption>();
        }

        // A vehicle is available when none of its NON-cancelled contracts (except the current one if editing) overlaps the period.
        var vehicles = await _db.Vehicles
            .AsNoTracking()
            .Where(v => !v.Contracts.Any(c => (ignoreContractId == null || c.Id != ignoreContractId.Value)
                                              && c.CancelledOn == null
                                              && c.StartDate <= endDate
                                              && c.EndDate >= startDate))
            .OrderBy(v => v.Brand).ThenBy(v => v.Model)
            .Select(v => new { v.Id, v.Brand, v.Model, v.LicensePlate })
            .ToListAsync();

        return vehicles
            .Select(v => new VehicleOption(v.Id, $"{v.Brand} {v.Model} ({v.LicensePlate})"))
            .ToList();
    }

    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    // Checks that do not need the database.
    private static OperationResult ValidateInput(ContractInput input, DateOnly today)
    {
        var result = new OperationResult();

        if (input.CustomerId <= 0)
            result.AddError(nameof(ContractInput.CustomerId), "Selecione um cliente.");

        if (input.VehicleId <= 0)
            result.AddError(nameof(ContractInput.VehicleId), "Selecione um veículo.");

        if (input.StartDate < today)
            result.AddError(nameof(ContractInput.StartDate), "A data de início não pode ser anterior a hoje.");

        if (input.EndDate < input.StartDate)
            result.AddError(nameof(ContractInput.EndDate), "A data de fim não pode ser anterior à data de início.");

        if (input.InitialMileage < 0)
            result.AddError(nameof(ContractInput.InitialMileage), "A quilometragem inicial não pode ser negativa.");

        return result;
    }

    // Checks that need the database: both records exist and the vehicle is free in the period.
    private async Task ValidateAgainstDatabaseAsync(ContractInput input, OperationResult validation, int? ignoreContractId = null)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == input.CustomerId);
        if (!customerExists)
            validation.AddError(nameof(ContractInput.CustomerId), "O cliente selecionado não existe.");

        var vehicleExists = await _db.Vehicles.AnyAsync(v => v.Id == input.VehicleId);
        if (!vehicleExists)
            validation.AddError(nameof(ContractInput.VehicleId), "O veículo selecionado não existe.");

        if (!vehicleExists)
        {
            return;
        }

        // Two periods overlap when each one starts before (or on the day) the other ends.
        // Dates are inclusive, so a contract ending on the 10th blocks one starting on the 10th.
        // CHANGED: cancelled contracts are ignored, and when updating, the current contract is ignored.
        var conflict = await _db.RentalContracts
            .AsNoTracking()
            .Where(c => (ignoreContractId == null || c.Id != ignoreContractId.Value)
                        && c.VehicleId == input.VehicleId
                        && c.CancelledOn == null
                        && c.StartDate <= input.EndDate
                        && c.EndDate >= input.StartDate)
            .OrderBy(c => c.StartDate)
            .Select(c => new { c.StartDate, c.EndDate })
            .FirstOrDefaultAsync();

        if (conflict is not null)
        {
            validation.AddError(
                nameof(ContractInput.VehicleId),
                $"Este veículo já está alugado entre {Format(conflict.StartDate)} e {Format(conflict.EndDate)}.");
        }
    }

    // Joins customer and vehicle in a single query (no N+1) and calculates the status in memory.
    private async Task<List<ContractListItem>> QueryAsync(IQueryable<RentalContract> contracts)
    {
        var today = Today();

        var rows = await contracts
            .AsNoTracking()
            .OrderByDescending(c => c.StartDate).ThenByDescending(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.CustomerId,
                CustomerName = c.Customer.FullName,
                c.VehicleId,
                c.Vehicle.Brand,
                c.Vehicle.Model,
                c.Vehicle.LicensePlate,
                c.StartDate,
                c.EndDate,
                c.InitialMileage,
                c.CancelledOn // CHANGED
            })
            .ToListAsync();

        return rows
            .Select(r => new ContractListItem(
                r.Id,
                r.CustomerId,
                r.CustomerName,
                r.VehicleId,
                $"{r.Brand} {r.Model}",
                r.LicensePlate,
                r.StartDate,
                r.EndDate,
                r.InitialMileage,
                r.CancelledOn, // CHANGED
                GetStatus(r.StartDate, r.EndDate, r.CancelledOn, today)))
            .ToList();
    }

    // CHANGED: a cancelled contract is always "Cancelled", whatever its dates say.
    // End date is inclusive: on the last day the contract is still active.
    private static ContractStatus GetStatus(DateOnly start, DateOnly end, DateOnly? cancelledOn, DateOnly today) =>
        cancelledOn.HasValue ? ContractStatus.Cancelled
        : today < start ? ContractStatus.Upcoming
        : today > end ? ContractStatus.Finished
        : ContractStatus.Active;

    private static string Format(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

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
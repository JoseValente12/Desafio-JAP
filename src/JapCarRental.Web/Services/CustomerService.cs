using System.Net.Mail;
using System.Text.RegularExpressions;
using JapCarRental.Web.Data;
using JapCarRental.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Web.Services;

public class CustomerService : ICustomerService
{
    // Optional leading +, then 9 to 15 digits (Portuguese and international numbers).
    private static readonly Regex PhoneFormat = new(@"^\+?\d{9,15}$");

    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerListItem>> GetAllAsync()
    {
        return await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerListItem(
                c.Id,
                c.FullName,
                c.Email,
                c.PhoneNumber,
                c.DrivingLicenseNumber,
                c.Contracts.Count))
            .ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id) => await _db.Customers.FindAsync(id);

    public async Task<OperationResult<int>> CreateAsync(CustomerInput input)
    {
        var validation = ValidateInput(input);
        var email = NormalizeEmail(input.Email);

        if (validation.Succeeded && await EmailExistsAsync(email, ignoreId: null))
        {
            validation.AddError(nameof(CustomerInput.Email), "Já existe um cliente com este email.");
        }

        if (!validation.Succeeded)
        {
            return Fail<int>(validation);
        }

        var customer = new Customer(
            input.FullName.Trim(),
            email,
            NormalizePhone(input.PhoneNumber),
            NormalizeLicense(input.DrivingLicenseNumber));

        _db.Customers.Add(customer);

        try
        {
            // The unique index on Email is the last guard against two simultaneous requests.
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(customer).State = EntityState.Detached;

            var emailTaken = await _db.Customers.AnyAsync(c => c.Email == customer.Email);
            if (!emailTaken) throw;

            var result = new OperationResult<int>();
            result.AddError(nameof(CustomerInput.Email), "Já existe um cliente com este email.");
            return result;
        }

        return OperationResult<int>.Success(customer.Id);
    }

    public async Task<OperationResult> UpdateAsync(int id, CustomerInput input)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null)
        {
            return OperationResult.Failure("Cliente não encontrado.");
        }

        var validation = ValidateInput(input);
        var email = NormalizeEmail(input.Email);

        // The customer being edited may keep their own email, so it is ignored in the check.
        if (validation.Succeeded && await EmailExistsAsync(email, ignoreId: id))
        {
            validation.AddError(nameof(CustomerInput.Email), "Já existe outro cliente com este email.");
        }

        if (!validation.Succeeded)
        {
            return validation;
        }

        customer.UpdateDetails(
            input.FullName.Trim(),
            email,
            NormalizePhone(input.PhoneNumber),
            NormalizeLicense(input.DrivingLicenseNumber));

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            _db.Entry(customer).State = EntityState.Unchanged;

            var emailTaken = await _db.Customers.AnyAsync(c => c.Id != id && c.Email == customer.Email);
            if (!emailTaken) throw;

            var result = new OperationResult();
            result.AddError(nameof(CustomerInput.Email), "Já existe outro cliente com este email.");
            return result;
        }

        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null)
        {
            return OperationResult.Failure("Cliente não encontrado.");
        }

        // Business rule: the history of contracts must be kept (the FK is also Restrict).
        if (await _db.RentalContracts.AnyAsync(c => c.CustomerId == id))
        {
            return OperationResult.Failure("Não é possível apagar um cliente que tem contratos.");
        }

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();

        return OperationResult.Success();
    }

    // Checks that do not need the database. Lengths match the Fluent API configuration.
    private static OperationResult ValidateInput(CustomerInput input)
    {
        var result = new OperationResult();

        if (string.IsNullOrWhiteSpace(input.FullName))
            result.AddError(nameof(CustomerInput.FullName), "O nome é obrigatório.");
        else if (input.FullName.Trim().Length > 150)
            result.AddError(nameof(CustomerInput.FullName), "O nome não pode ter mais de 150 caracteres.");

        var email = NormalizeEmail(input.Email);
        if (email.Length == 0)
            result.AddError(nameof(CustomerInput.Email), "O email é obrigatório.");
        else if (email.Length > 254 || !IsValidEmail(email))
            result.AddError(nameof(CustomerInput.Email), "O email não é válido.");

        if (string.IsNullOrWhiteSpace(input.PhoneNumber))
            result.AddError(nameof(CustomerInput.PhoneNumber), "O telefone é obrigatório.");
        else if (!PhoneFormat.IsMatch(NormalizePhone(input.PhoneNumber)))
            result.AddError(nameof(CustomerInput.PhoneNumber), "O telefone deve ter entre 9 e 15 dígitos.");

        if (string.IsNullOrWhiteSpace(input.DrivingLicenseNumber))
            result.AddError(nameof(CustomerInput.DrivingLicenseNumber), "O número da carta de condução é obrigatório.");
        else if (input.DrivingLicenseNumber.Trim().Length > 30)
            result.AddError(nameof(CustomerInput.DrivingLicenseNumber), "O número da carta não pode ter mais de 30 caracteres.");

        return result;
    }

    // Emails are stored in lower case, so "Ana@Example.com" and "ana@example.com" are the same customer.
    private static string NormalizeEmail(string? raw) => (raw ?? string.Empty).Trim().ToLowerInvariant();

    // "910 000 001" and "910-000-001" become "910000001". A leading + is kept.
    private static string NormalizePhone(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        var digits = new string(value.Where(char.IsDigit).ToArray());

        return value.StartsWith('+') ? "+" + digits : digits;
    }

    private static string NormalizeLicense(string? raw) => (raw ?? string.Empty).Trim().ToUpperInvariant();

    // MailAddress parses "Name <a@b.com>" too, so the address must equal the input exactly.
    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out var address)
        && address.Address == email
        && address.Host.Contains('.');

    private Task<bool> EmailExistsAsync(string email, int? ignoreId)
    {
        // When creating there is no customer to ignore. When editing, the customer being
        // edited may keep their own email, so their Id is excluded from the check.
        return _db.Customers.AnyAsync(c =>
            c.Email == email &&
            (!ignoreId.HasValue || c.Id != ignoreId.Value));
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
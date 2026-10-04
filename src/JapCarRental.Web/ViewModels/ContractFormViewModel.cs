using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JapCarRental.Web.ViewModels;

// Form model for creating a contract. Contracts are never edited, so there is no Id.
// Nullable ids and dates let [Required] tell "nothing chosen" from a real value.
public class ContractFormViewModel
{
    [Required(ErrorMessage = "Selecione um cliente.")]
    [Display(Name = "Cliente")]
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Selecione um veículo.")]
    [Display(Name = "Veículo")]
    public int? VehicleId { get; set; }

    [Required(ErrorMessage = "Indique a data de início.")]
    [Display(Name = "Data de início")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Indique a data de fim.")]
    [Display(Name = "Data de fim")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "A quilometragem inicial não pode ser negativa.")]
    [Display(Name = "Quilometragem inicial (km)")]
    public int InitialMileage { get; set; }

    // Dropdown options. The controller fills them on every render and they are never read
    // from the request (BindNever), so a client cannot inject its own list.
    [BindNever] public IReadOnlyList<SelectListItem> Customers { get; set; } = [];
    [BindNever] public IReadOnlyList<SelectListItem> Vehicles { get; set; } = [];

    // Overlap, past dates and "end after start" live in ContractService, the single source of truth.
}
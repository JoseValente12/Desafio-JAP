using System.ComponentModel.DataAnnotations;
using JapCarRental.Web.Models;

namespace JapCarRental.Web.ViewModels;

// Form model for create and edit. It is separate from the Vehicle entity on purpose:
// the entity has private setters and its own rules, and binding the request straight onto it
// would allow over-posting (a client sending fields we never meant to expose).
public class VehicleFormViewModel
{
    // Only used by Edit. Create leaves it at 0.
    public int Id { get; set; }

    [Required(ErrorMessage = "A marca é obrigatória.")]
    [StringLength(50, ErrorMessage = "A marca pode ter no máximo 50 caracteres.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "O modelo é obrigatório.")]
    [StringLength(50, ErrorMessage = "O modelo pode ter no máximo 50 caracteres.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "A matrícula é obrigatória.")]
    [Display(Name = "Matrícula")]
    public string LicensePlate { get; set; } = string.Empty;

    [Display(Name = "Ano de fabrico")]
    [Range(1900, 2100, ErrorMessage = "Indique um ano de fabrico válido.")]
    public int ManufactureYear { get; set; } = DateTime.Today.Year;

    [Display(Name = "Combustível")]
    public FuelType FuelType { get; set; } = FuelType.Petrol;

    // The service stays the single source of truth for business rules (future year, duplicate plate).
    // These annotations only give fast feedback on obviously wrong input.
}
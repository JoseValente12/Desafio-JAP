using System.ComponentModel.DataAnnotations;

namespace JapCarRental.Web.ViewModels;

// Form model for create and edit. Separate from the Customer entity (private setters)
// so a request can only ever set the fields we expose here.
public class CustomerFormViewModel
{
    // Only used by the view to know it is editing. The controller always trusts the route id.
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(150, ErrorMessage = "O nome pode ter no máximo 150 caracteres.")]
    [Display(Name = "Nome completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [StringLength(254, ErrorMessage = "O email pode ter no máximo 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [StringLength(20, ErrorMessage = "O telefone pode ter no máximo 20 caracteres.")]
    [Display(Name = "Telefone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "O número da carta de condução é obrigatório.")]
    [StringLength(30, ErrorMessage = "O número da carta pode ter no máximo 30 caracteres.")]
    [Display(Name = "Carta de condução")]
    public string DrivingLicenseNumber { get; set; } = string.Empty;

    // Format rules (email shape, phone digits) and the unique email live in CustomerService.
}
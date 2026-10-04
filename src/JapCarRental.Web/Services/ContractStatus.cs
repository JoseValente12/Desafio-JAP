using System.ComponentModel.DataAnnotations;

namespace JapCarRental.Web.Services;

/// <summary>
/// Status of a contract on a given day. Calculated from the dates, never stored.
/// </summary>
public enum ContractStatus
{
    [Display(Name = "Agendado")]
    Upcoming = 1,

    [Display(Name = "Ativo")]
    Active = 2,

    [Display(Name = "Terminado")]
    Finished = 3,

    [Display(Name = "Cancelado")]
    Cancelled = 4
}
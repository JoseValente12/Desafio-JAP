using System.ComponentModel.DataAnnotations;

namespace JapCarRental.Web.Models;

public enum FuelType
{
    [Display(Name = "Gasolina")]
    Petrol = 1,

    [Display(Name = "Gasóleo / Diesel")]
    Diesel = 2,

    [Display(Name = "Elétrico")]
    Electric = 3,

    [Display(Name = "Híbrido")]
    Hybrid = 4,

    [Display(Name = "GPL")]
    Lpg = 5
}
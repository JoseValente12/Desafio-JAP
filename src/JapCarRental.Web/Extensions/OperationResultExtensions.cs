using JapCarRental.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JapCarRental.Web.Extensions;

public static class OperationResultExtensions
{
    /// <summary>
    /// Copies the errors of a failed operation into the ModelState, so the views
    /// show them next to the right inputs (or in the validation summary for general errors).
    /// Lives in the web layer on purpose: the services must not depend on MVC types.
    /// </summary>
    public static void AddToModelState(this OperationResult result, ModelStateDictionary modelState)
    {
        foreach (var (field, messages) in result.Errors)
        {
            foreach (var message in messages)
            {
                modelState.AddModelError(field, message);
            }
        }
    }
}
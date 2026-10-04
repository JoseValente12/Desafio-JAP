using System.Diagnostics;
using JapCarRental.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JapCarRental.Web.Controllers;

public class ErrorController : Controller
{
    // Unhandled exceptions (500). The exception handler middleware already set the status code.
    [Route("Error")]
    public IActionResult Index() => View("Status", Build(500));

    // Status codes such as 404, re-executed by UseStatusCodePagesWithReExecute.
    [Route("Error/{statusCode:int}")]
    public IActionResult Status(int statusCode) => View("Status", Build(statusCode));

    private ErrorViewModel Build(int statusCode)
    {
        // The id lets the user quote it to support, and lets us find the entry in the logs.
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var (title, message) = statusCode switch
        {
            404 => ("Página não encontrada", "A página que procura não existe ou foi movida."),
            400 => ("Pedido inválido", "Não foi possível processar o pedido."),
            >= 500 => ("Algo correu mal", "Ocorreu um erro inesperado. Tente novamente dentro de instantes."),
            _ => ("Ocorreu um erro", "Não foi possível concluir o pedido.")
        };

        return new ErrorViewModel(statusCode, title, message, requestId);
    }
}
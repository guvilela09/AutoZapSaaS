using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class HistoricoController : Controller
{
    private readonly AutoZapApiClient _api;

    public HistoricoController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? filtro)
    {
        var modelo = new HistoricoViewModel { Filtro = filtro };

        try
        {
            var todas = await _api.ListarMensagensAsync();

            modelo.TotalEnviadas = todas.Count(m => EhSucesso(m.Status));
            modelo.TotalFalhas = todas.Count(m => EhFalha(m.Status));

            modelo.Mensagens = filtro?.ToLowerInvariant() switch
            {
                "falhas" => todas.Where(m => EhFalha(m.Status)).ToList(),
                "enviadas" => todas.Where(m => EhSucesso(m.Status)).ToList(),
                _ => todas
            };
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    private static bool EhSucesso(string status) =>
        status.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);

    private static bool EhFalha(string status) =>
        status.Equals("Failed", StringComparison.OrdinalIgnoreCase);
}

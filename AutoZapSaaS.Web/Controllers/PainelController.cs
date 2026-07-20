using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class PainelController : Controller
{
    private readonly AutoZapApiClient _api;

    public PainelController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var modelo = new PainelViewModel();

        try
        {
            modelo.Assinatura = await _api.ObterAssinaturaAsync();
            modelo.Instancias = await _api.ListarInstanciasAsync();
            modelo.TotalClientes = (await _api.ListarClientesAsync()).Count;
            modelo.TemIntegracao = (await _api.ListarIntegracoesAsync()).Any();
            modelo.TemTemplate = (await _api.ListarTemplatesAsync()).Any();

            var mensagens = await _api.ListarMensagensAsync();
            modelo.MensagensEnviadas = mensagens.Count(m =>
                m.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
                m.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase));
            modelo.MensagensComFalha = mensagens.Count(m =>
                m.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Erro() => View();
}

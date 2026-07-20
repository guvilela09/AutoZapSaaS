using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class WebhooksController : Controller
{
    private readonly AutoZapApiClient _api;

    public WebhooksController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var modelo = new WebhooksViewModel();

        try
        {
            modelo.Integracoes = await _api.ListarIntegracoesAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string plataforma, string segredo)
    {
        if (string.IsNullOrWhiteSpace(segredo))
        {
            TempData["Erro"] = "Informe o segredo de assinatura da plataforma.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.CriarIntegracaoAsync(plataforma, segredo.Trim());
            TempData["Sucesso"] = "Integração criada. Copie a URL abaixo e cadastre na sua plataforma.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Girar(Guid id)
    {
        try
        {
            await _api.GirarTokenAsync(id);
            TempData["Sucesso"] = "Novo endereço gerado. Atualize a URL na sua plataforma — a anterior parou de funcionar.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remover(Guid id)
    {
        try
        {
            await _api.RemoverIntegracaoAsync(id);
            TempData["Sucesso"] = "Integração removida.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class TemplatesController : Controller
{
    private readonly AutoZapApiClient _api;

    public TemplatesController(AutoZapApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var modelo = new TemplatesViewModel();

        try
        {
            modelo.Templates = await _api.ListarTemplatesAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string nome, string evento, string corpo)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(corpo))
        {
            TempData["Erro"] = "Preencha o nome e o texto da mensagem.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _api.CriarTemplateAsync(nome.Trim(), evento, corpo.Trim());
            TempData["Sucesso"] = "Mensagem automática criada.";
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
            await _api.RemoverTemplateAsync(id);
            TempData["Sucesso"] = "Mensagem removida.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

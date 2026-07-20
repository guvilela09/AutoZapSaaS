using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoZapSaaS.Web.Controllers;

public class InstanciasController : Controller
{
    private readonly AutoZapApiClient _api;
    private readonly ILogger<InstanciasController> _logger;

    public InstanciasController(AutoZapApiClient api, ILogger<InstanciasController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index(bool primeiroAcesso = false)
    {
        var modelo = new InstanciasViewModel { PrimeiroAcesso = primeiroAcesso };

        try
        {
            modelo.Instancias = await _api.ListarInstanciasAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        // O limite de plano vem do POST anterior via TempData; a view usa para
        // oferecer o upgrade junto do erro.
        modelo.PlanoSugerido = TempData["PlanoSugerido"] as string;

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "Dê um nome para identificar este número.";
            return RedirectToAction(nameof(Index));
        }

        // sessionName precisa ser unico na Evolution entre todos os tenants.
        var sessao = $"{Slug(nome)}-{Guid.NewGuid().ToString("n")[..8]}";
        var token = Guid.NewGuid().ToString("n");

        try
        {
            var criada = await _api.CriarInstanciaAsync(nome.Trim(), sessao, token);
            return RedirectToAction(nameof(Conectar), new { id = criada.Id });
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
            TempData["PlanoSugerido"] = ex.PlanoSugerido;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Conectar(Guid id)
    {
        InstanceResponse? instancia;

        try
        {
            instancia = (await _api.ListarInstanciasAsync()).FirstOrDefault(i => i.Id == id);
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        if (instancia is null)
        {
            TempData["Erro"] = "Número não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = new ConectarViewModel { Instancia = instancia };

        try
        {
            modelo.QrCodeBase64 = (await _api.ObterQrCodeAsync(id)).QrCodeBase64;
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return View(modelo);
    }

    /// <summary>
    /// Consultado por JavaScript a cada poucos segundos: o lojista escaneia o QR
    /// e a tela avança sozinha, sem precisar recarregar na mão.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Status(Guid id)
    {
        try
        {
            var status = await _api.ObterStatusAsync(id);
            return Json(new { status = status.Status });
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Falha ao consultar status de {Id}: {Msg}", id, ex.Message);
            return Json(new { status = "desconhecido" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remover(Guid id)
    {
        try
        {
            await _api.RemoverInstanciaAsync(id);
            TempData["Sucesso"] = "Número removido.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desconectar(Guid id)
    {
        try
        {
            await _api.DesconectarInstanciaAsync(id);
            TempData["Sucesso"] = "Número desconectado.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private static string Slug(string valor)
    {
        var limpo = new string(valor.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        return limpo.Trim('-') is { Length: > 0 } s ? s : "zap";
    }
}

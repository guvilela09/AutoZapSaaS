using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class InstanciasModel : PageModel
{
    private readonly AutoZapApiClient _api;
    private readonly ILogger<InstanciasModel> _logger;

    public InstanciasModel(AutoZapApiClient api, ILogger<InstanciasModel> logger)
    {
        _api = api;
        _logger = logger;
    }

    public List<InstanceResponse> Instancias { get; private set; } = new();

    [BindProperty]
    public string Nome { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public bool PrimeiroAcesso { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Instancias = await _api.ListarInstanciasAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            TempData["Erro"] = "Dê um nome para identificar este número.";
            return RedirectToPage();
        }

        // sessionName precisa ser unico na Evolution entre todos os tenants.
        var sessao = $"{Slug(Nome)}-{Guid.NewGuid().ToString("n")[..8]}";
        var token = Guid.NewGuid().ToString("n");

        try
        {
            var criada = await _api.CriarInstanciaAsync(Nome.Trim(), sessao, token);
            return RedirectToPage("/Conectar", new { id = criada.Id });
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
            TempData["PlanoSugerido"] = ex.PlanoSugerido;

            // Redireciona em vez de devolver Page(): sem isso, um F5 reenviaria
            // o formulario e criaria outra instancia.
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostRemoverAsync(Guid id)
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

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDesconectarAsync(Guid id)
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

        return RedirectToPage();
    }

    private static string Slug(string valor)
    {
        var limpo = new string(valor.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        return limpo.Trim('-') is { Length: > 0 } s ? s : "zap";
    }

    public static string SeloDeStatus(string status) => status.ToLowerInvariant() switch
    {
        "connected" or "open" => "selo-ok",
        "connecting" => "selo-atencao",
        "banned" => "selo-erro",
        _ => "selo-neutro"
    };

    public static string TextoDeStatus(string status) => status.ToLowerInvariant() switch
    {
        "connected" or "open" => "Conectado",
        "connecting" => "Aguardando leitura",
        "banned" => "Bloqueado",
        _ => "Desconectado"
    };
}

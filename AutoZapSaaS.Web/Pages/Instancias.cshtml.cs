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

    /// <summary>Instância cujo QR Code está sendo exibido.</summary>
    public InstanceResponse? ConectandoInstancia { get; private set; }
    public string? QrCodeBase64 { get; private set; }

    public string? Erro { get; set; }
    public string? Sucesso { get; set; }

    /// <summary>Quando o erro é limite de plano, a página oferece o upgrade.</summary>
    public string? PlanoSugerido { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            Erro = "Dê um nome para identificar este número.";
            await CarregarAsync();
            return Page();
        }

        // sessionName precisa ser único na Evolution entre todos os tenants.
        var sessao = $"{Slug(Nome)}-{Guid.NewGuid().ToString("n")[..8]}";
        var token = Guid.NewGuid().ToString("n");

        try
        {
            var criada = await _api.CriarInstanciaAsync(Nome.Trim(), sessao, token);
            return RedirectToPage(new { conectar = criada.Id });
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
            PlanoSugerido = ex.PlanoSugerido;
            await CarregarAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnGetConectarAsync(Guid conectar)
    {
        await CarregarAsync();

        ConectandoInstancia = Instancias.FirstOrDefault(i => i.Id == conectar);
        if (ConectandoInstancia is null)
        {
            Erro = "Número não encontrado.";
            return Page();
        }

        try
        {
            var qr = await _api.ObterQrCodeAsync(conectar);
            QrCodeBase64 = qr.QrCodeBase64;
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        return Page();
    }

    /// <summary>
    /// Consultado por JavaScript a cada poucos segundos: o lojista escaneia o QR
    /// e a tela avança sozinha, sem precisar recarregar na mão.
    /// </summary>
    public async Task<IActionResult> OnGetStatusAsync(Guid id)
    {
        try
        {
            var status = await _api.ObterStatusAsync(id);
            return new JsonResult(new { status = status.Status });
        }
        catch (ApiException ex)
        {
            _logger.LogWarning("Falha ao consultar status de {Id}: {Msg}", id, ex.Message);
            return new JsonResult(new { status = "desconhecido" });
        }
    }

    public async Task<IActionResult> OnPostRemoverAsync(Guid id)
    {
        try
        {
            await _api.RemoverInstanciaAsync(id);
            Sucesso = "Número removido.";
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDesconectarAsync(Guid id)
    {
        try
        {
            await _api.DesconectarInstanciaAsync(id);
            Sucesso = "Número desconectado.";
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    private async Task CarregarAsync()
    {
        try
        {
            Instancias = await _api.ListarInstanciasAsync();
        }
        catch (ApiException ex)
        {
            Erro ??= ex.Message;
            Instancias = new List<InstanceResponse>();
        }
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

using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class PlanosModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public PlanosModel(AutoZapApiClient api) => _api = api;

    public List<PlanoResponse> Planos { get; private set; } = new();
    public AssinaturaResponse? Assinatura { get; private set; }

    [BindProperty] public string FormaPagamento { get; set; } = "Pix";

    public string? Erro { get; set; }
    public string? Sucesso { get; set; }

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostAssinarAsync(string tier)
    {
        try
        {
            await _api.AssinarAsync(tier, FormaPagamento);
            Sucesso = "Assinatura criada. Assim que o pagamento for confirmado, " +
                      "os novos limites entram em vigor.";
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCancelarAsync()
    {
        try
        {
            await _api.CancelarAssinaturaAsync();
            Sucesso = "Assinatura cancelada. Sua conta voltou para o plano Free.";
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
            Planos = await _api.ListarPlanosAsync();
            Assinatura = await _api.ObterAssinaturaAsync();
        }
        catch (ApiException ex)
        {
            Erro ??= ex.Message;
        }
    }

    public static string TextoDoStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" => "Ativa",
        "pending" => "Aguardando pagamento",
        "overdue" => "Pagamento atrasado",
        "suspended" => "Suspensa",
        "canceled" => "Cancelada",
        _ => status
    };

    public static string SeloDoStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" => "selo-ok",
        "pending" or "overdue" => "selo-atencao",
        "suspended" or "canceled" => "selo-erro",
        _ => "selo-neutro"
    };
}

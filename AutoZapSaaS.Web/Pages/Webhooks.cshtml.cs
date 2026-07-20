using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class WebhooksModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public WebhooksModel(AutoZapApiClient api) => _api = api;

    public List<WebhookIntegrationResponse> Integracoes { get; private set; } = new();

    [BindProperty] public string Plataforma { get; set; } = "Nuvemshop";
    [BindProperty] public string Segredo { get; set; } = string.Empty;

    public static readonly (string Valor, string Rotulo, string Onde)[] Plataformas =
    {
        ("Nuvemshop", "Nuvemshop", "Painel da Nuvemshop → Aplicativos → Webhooks"),
        ("Kiwify", "Kiwify", "Painel da Kiwify → Apps → Webhooks"),
        ("Hotmart", "Hotmart", "Painel da Hotmart → Ferramentas → Webhook")
    };

    public static string OndeConfigurar(string plataforma) =>
        Plataformas.FirstOrDefault(p =>
            p.Valor.Equals(plataforma, StringComparison.OrdinalIgnoreCase)).Onde ?? "";

    public async Task OnGetAsync()
    {
        try
        {
            Integracoes = await _api.ListarIntegracoesAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Segredo))
        {
            TempData["Erro"] = "Informe o segredo de assinatura da plataforma.";
            return RedirectToPage();
        }

        try
        {
            await _api.CriarIntegracaoAsync(Plataforma, Segredo.Trim());
            TempData["Sucesso"] = "Integração criada. Copie a URL abaixo e cadastre na sua plataforma.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGirarAsync(Guid id)
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

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoverAsync(Guid id)
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

        return RedirectToPage();
    }
}

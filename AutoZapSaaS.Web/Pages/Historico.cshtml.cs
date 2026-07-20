using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class HistoricoModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public HistoricoModel(AutoZapApiClient api) => _api = api;

    public List<WhatsAppMessageResponse> Mensagens { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string? Filtro { get; set; }

    public int TotalEnviadas { get; private set; }
    public int TotalFalhas { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var todas = await _api.ListarMensagensAsync();

            TotalEnviadas = todas.Count(m => EhSucesso(m.Status));
            TotalFalhas = todas.Count(m => EhFalha(m.Status));

            Mensagens = Filtro?.ToLowerInvariant() switch
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
    }

    private static bool EhSucesso(string status) =>
        status.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
        status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);

    private static bool EhFalha(string status) =>
        status.Equals("Failed", StringComparison.OrdinalIgnoreCase);

    public static string SeloDoStatus(string status) => status.ToLowerInvariant() switch
    {
        "sent" or "delivered" => "selo-ok",
        "failed" => "selo-erro",
        "queued" => "selo-atencao",
        _ => "selo-neutro"
    };

    public static string TextoDoStatus(string status) => status.ToLowerInvariant() switch
    {
        "sent" => "Enviada",
        "delivered" => "Entregue",
        "failed" => "Falhou",
        "queued" => "Na fila",
        _ => status
    };
}

using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class TemplatesModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public TemplatesModel(AutoZapApiClient api) => _api = api;

    public List<MessageTemplateResponse> Templates { get; private set; } = new();

    [BindProperty] public string Nome { get; set; } = string.Empty;
    [BindProperty] public string Evento { get; set; } = "OrderCreated";
    [BindProperty] public string Corpo { get; set; } = string.Empty;

    /// <summary>Eventos aceitos pela API, com rotulo que o lojista entende.</summary>
    public static readonly (string Valor, string Rotulo)[] Eventos =
    {
        ("OrderCreated", "Pedido criado"),
        ("PaymentConfirmed", "Pagamento confirmado"),
        ("OrderShipped", "Pedido enviado"),
        ("OrderCanceled", "Pedido cancelado"),
        ("CartAbandoned", "Carrinho abandonado")
    };

    public static string RotuloDoEvento(string valor) =>
        Eventos.FirstOrDefault(e => e.Valor.Equals(valor, StringComparison.OrdinalIgnoreCase)).Rotulo ?? valor;

    public async Task OnGetAsync()
    {
        try
        {
            Templates = await _api.ListarTemplatesAsync();
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Corpo))
        {
            TempData["Erro"] = "Preencha o nome e o texto da mensagem.";
            return RedirectToPage();
        }

        try
        {
            await _api.CriarTemplateAsync(Nome.Trim(), Evento, Corpo.Trim());
            TempData["Sucesso"] = "Mensagem automática criada.";
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
            await _api.RemoverTemplateAsync(id);
            TempData["Sucesso"] = "Mensagem removida.";
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToPage();
    }
}

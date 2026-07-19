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

    public string? Erro { get; set; }
    public string? Sucesso { get; set; }

    /// <summary>Eventos aceitos pela API, com rótulo que o lojista entende.</summary>
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

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostCriarAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Corpo))
        {
            Erro = "Preencha o nome e o texto da mensagem.";
            await CarregarAsync();
            return Page();
        }

        try
        {
            await _api.CriarTemplateAsync(Nome.Trim(), Evento, Corpo.Trim());
            Sucesso = "Mensagem automática criada.";
            Nome = Corpo = string.Empty;
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }

        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoverAsync(Guid id)
    {
        try
        {
            await _api.RemoverTemplateAsync(id);
            Sucesso = "Mensagem removida.";
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
            Templates = await _api.ListarTemplatesAsync();
        }
        catch (ApiException ex)
        {
            Erro ??= ex.Message;
        }
    }
}

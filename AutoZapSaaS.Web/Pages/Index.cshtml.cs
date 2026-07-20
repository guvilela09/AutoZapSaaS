using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public IndexModel(AutoZapApiClient api) => _api = api;

    public PainelViewModel Dados { get; private set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Dados.Assinatura = await _api.ObterAssinaturaAsync();
            Dados.Instancias = await _api.ListarInstanciasAsync();
            Dados.TotalClientes = (await _api.ListarClientesAsync()).Count;
            Dados.TemIntegracao = (await _api.ListarIntegracoesAsync()).Any();
            Dados.TemTemplate = (await _api.ListarTemplatesAsync()).Any();

            var mensagens = await _api.ListarMensagensAsync();
            Dados.MensagensEnviadas = mensagens.Count(m =>
                m.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
                m.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase));
            Dados.MensagensComFalha = mensagens.Count(m =>
                m.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
        }
        catch (ApiException ex)
        {
            TempData["Erro"] = ex.Message;
        }
    }
}

using AutoZapSaaS.Web.Models;
using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutoZapSaaS.Web.Pages;

public class IndexModel : PageModel
{
    private readonly AutoZapApiClient _api;

    public IndexModel(AutoZapApiClient api) => _api = api;

    public AssinaturaResponse? Assinatura { get; private set; }
    public List<InstanceResponse> Instancias { get; private set; } = new();
    public int TotalClientes { get; private set; }
    public int MensagensEnviadas { get; private set; }
    public int MensagensComFalha { get; private set; }
    public string? Erro { get; set; }

    public bool TemNumeroConectado =>
        Instancias.Any(i => i.Status.Equals("Connected", StringComparison.OrdinalIgnoreCase));

    public bool TemIntegracao { get; private set; }
    public bool TemTemplate { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Assinatura = await _api.ObterAssinaturaAsync();
            Instancias = await _api.ListarInstanciasAsync();
            TotalClientes = (await _api.ListarClientesAsync()).Count;
            TemIntegracao = (await _api.ListarIntegracoesAsync()).Any();
            TemTemplate = (await _api.ListarTemplatesAsync()).Any();

            var mensagens = await _api.ListarMensagensAsync();
            MensagensEnviadas = mensagens.Count(m =>
                m.Status.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
                m.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase));
            MensagensComFalha = mensagens.Count(m =>
                m.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
        }
        catch (ApiException ex)
        {
            Erro = ex.Message;
        }
    }

    /// <summary>Percentual da cota usada, para o medidor da tela.</summary>
    public int PercentualUsado()
    {
        if (Assinatura is null || Assinatura.MaxMensagensPorMes == 0) return 0;

        var usado = Assinatura.MaxMensagensPorMes - Assinatura.MensagensRestantes;
        return (int)Math.Round(usado * 100.0 / Assinatura.MaxMensagensPorMes);
    }
}

using System.ComponentModel.DataAnnotations;

namespace AutoZapSaaS.Web.Models;

// ViewModels do painel. Separados dos contratos da API (ApiModels) porque
// carregam validacao e estado de tela — coisas que a API nao conhece.

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    public string Senha { get; set; } = string.Empty;
}

public class CadastroViewModel
{
    [Required(ErrorMessage = "Informe o nome da loja")]
    public string NomeEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail da loja")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CPF ou CNPJ")]
    public string Documento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail de acesso")]
    [EmailAddress(ErrorMessage = "E-mail invalido")]
    public string EmailAdmin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [MinLength(8, ErrorMessage = "A senha precisa ter ao menos 8 caracteres")]
    public string SenhaAdmin { get; set; } = string.Empty;
}

public class PainelViewModel
{
    public AssinaturaResponse? Assinatura { get; set; }
    public List<InstanceResponse> Instancias { get; set; } = new();
    public int TotalClientes { get; set; }
    public int MensagensEnviadas { get; set; }
    public int MensagensComFalha { get; set; }

    public bool TemNumeroConectado =>
        Instancias.Any(i => i.Status.Equals("Connected", StringComparison.OrdinalIgnoreCase));

    public bool TemIntegracao { get; set; }
    public bool TemTemplate { get; set; }

    /// <summary>Percentual da cota usada, para o medidor da tela.</summary>
    public int PercentualUsado()
    {
        if (Assinatura is null || Assinatura.MaxMensagensPorMes == 0) return 0;

        var usado = Assinatura.MaxMensagensPorMes - Assinatura.MensagensRestantes;
        return (int)Math.Round(usado * 100.0 / Assinatura.MaxMensagensPorMes);
    }
}

public class InstanciasViewModel
{
    public List<InstanceResponse> Instancias { get; set; } = new();
    public string Nome { get; set; } = string.Empty;
    public bool PrimeiroAcesso { get; set; }

    /// <summary>Preenchido quando o erro foi limite de plano, para oferecer o upgrade.</summary>
    public string? PlanoSugerido { get; set; }

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

public class ConectarViewModel
{
    public InstanceResponse Instancia { get; set; } = null!;
    public string? QrCodeBase64 { get; set; }
}

public class TemplatesViewModel
{
    public List<MessageTemplateResponse> Templates { get; set; } = new();
    public string Nome { get; set; } = string.Empty;
    public string Evento { get; set; } = "OrderCreated";
    public string Corpo { get; set; } = string.Empty;

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
}

public class ClientesViewModel
{
    public List<CustomerResponse> Clientes { get; set; } = new();
    public string Nome { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Busca { get; set; }

    public static string FormatarTelefone(string telefone)
    {
        var d = new string(telefone.Where(char.IsDigit).ToArray());

        // 55 + DDD + 9 digitos
        if (d.Length == 13 && d.StartsWith("55"))
            return $"+55 ({d[2..4]}) {d[4..9]}-{d[9..]}";

        if (d.Length == 11)
            return $"({d[..2]}) {d[2..7]}-{d[7..]}";

        return telefone;
    }
}

public class HistoricoViewModel
{
    public List<WhatsAppMessageResponse> Mensagens { get; set; } = new();
    public string? Filtro { get; set; }
    public int TotalEnviadas { get; set; }
    public int TotalFalhas { get; set; }

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

public class WebhooksViewModel
{
    public List<WebhookIntegrationResponse> Integracoes { get; set; } = new();
    public string Plataforma { get; set; } = "Nuvemshop";
    public string Segredo { get; set; } = string.Empty;

    public static readonly (string Valor, string Rotulo, string Onde)[] Plataformas =
    {
        ("Nuvemshop", "Nuvemshop", "Painel da Nuvemshop → Aplicativos → Webhooks"),
        ("Kiwify", "Kiwify", "Painel da Kiwify → Apps → Webhooks"),
        ("Hotmart", "Hotmart", "Painel da Hotmart → Ferramentas → Webhook")
    };

    public static string OndeConfigurar(string plataforma) =>
        Plataformas.FirstOrDefault(p =>
            p.Valor.Equals(plataforma, StringComparison.OrdinalIgnoreCase)).Onde ?? "";
}

public class PlanosViewModel
{
    public List<PlanoResponse> Planos { get; set; } = new();
    public AssinaturaResponse? Assinatura { get; set; }
    public string FormaPagamento { get; set; } = "Pix";

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

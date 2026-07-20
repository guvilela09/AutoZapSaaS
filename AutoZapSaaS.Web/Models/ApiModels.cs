namespace AutoZapSaaS.Web.Models;

// Espelham os contratos da API. Ficam aqui, e nao via referencia ao projeto
// Application, para que o painel dependa apenas do contrato HTTP publico —
// e possa ser publicado separado da API.

public record LoginResponse(string Token, string Email, string Role, Guid TenantId);

public record InstanceResponse(
    Guid Id, Guid TenantId, string Name, string SessionName,
    string Token, string Status, DateTime CreatedAt);

public record QrCodeResponse(string QrCodeBase64);

public record ConnectionStatusResponse(string Status);

public record CustomerResponse(
    Guid Id, Guid TenantId, string Name, string PhoneNumber,
    string Email, string Origin, DateTime CreatedAt);

public record MessageTemplateResponse(
    Guid Id, Guid TenantId, string Name, string EventType,
    string Body, bool IsActive, DateTime CreatedAt);

public record WhatsAppMessageResponse(
    Guid Id, string PhoneNumber, string Body, string Status,
    string CustomerName, DateTime CreatedAt);

public record WebhookIntegrationResponse(
    Guid Id, string Platform, string WebhookUrl, bool IsActive,
    DateTime? LastReceivedAt, DateTime CreatedAt);

public record PlanoResponse(
    string Tier, string Nome, decimal PrecoMensal,
    int MaxInstancias, int MaxMensagensPorMes, bool EhAtual);

public record AssinaturaResponse(
    string Tier, string PlanoNome, string Status, DateTime? PeriodoFimEm,
    int InstanciasUsadas, int MaxInstancias, int MensagensNoCiclo,
    int MaxMensagensPorMes, int MensagensRestantes);

/// <summary>
/// Erro devolvido pela API. O painel precisa distinguir limite de plano (402,
/// que vira convite de upgrade) de indisponibilidade (502, que vira "tente de novo").
/// </summary>
public record ApiErro(string Error, string? PlanoAtual = null, string? PlanoSugerido = null)
{
    public int StatusCode { get; init; }
    public bool EhLimiteDePlano => StatusCode == 402;
}

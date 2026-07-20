namespace AutoZapSaaS.Application.Common.Interfaces;

public record AsaasCliente(string Id);

public record AsaasAssinatura(string Id, DateTime? ProximoVencimento);

public enum AsaasFormaPagamento
{
    /// <summary>Deixa o cliente escolher entre Pix, boleto e cartão no checkout.</summary>
    Indefinida = 0,
    Boleto = 1,
    CartaoCredito = 2,
    Pix = 3
}

public interface IAsaasClient
{
    Task<AsaasCliente> CriarOuAtualizarClienteAsync(
        string nome, string email, string cpfCnpj, CancellationToken ct = default);

    Task<AsaasAssinatura> CriarAssinaturaAsync(
        string customerId, decimal valor, string descricao,
        AsaasFormaPagamento forma, CancellationToken ct = default);

    Task CancelarAssinaturaAsync(string subscriptionId, CancellationToken ct = default);
}

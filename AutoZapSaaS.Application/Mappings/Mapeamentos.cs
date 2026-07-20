using AutoZapSaaS.Application.DTOs;
using AutoZapSaaS.Domain.Entities;

namespace AutoZapSaaS.Application.Mappings;

/// <summary>
/// Conversao de entidade para DTO, escrita a mao.
///
/// Substituiu o AutoMapper: o pacote carrega um DoS de severidade alta
/// (GHSA-rvv3-g6hj-g44x) corrigido apenas a partir da 15.1.1, e a licenca deixou
/// de ser MIT na 15 — ou seja, corrigir exigiria licenca paga. Como sao sete
/// projecoes planas, mapear a mao sai mais barato, aparece no "Ir para definicao"
/// e quebra em tempo de compilacao quando um contrato muda, em vez de em runtime.
/// </summary>
public static class Mapeamentos
{
    public static CustomerResponse ParaResposta(this Customer c) => new(
        c.Id, c.TenantId, c.Name, c.PhoneNumber, c.Email, c.Origin, c.CreatedAt);

    public static InstanceResponse ParaResposta(this Instance i) => new(
        i.Id, i.TenantId, i.Name, i.SessionName, i.Token, i.Status.ToString(), i.CreatedAt);

    public static MessageTemplateResponse ParaResposta(this MessageTemplate t) => new(
        t.Id, t.TenantId, t.Name, t.EventType.ToString(), t.Body, t.IsActive, t.CreatedAt);


    public static WebhookEventResponse ParaResposta(this WebhookEvent w) => new(
        w.Id, w.Platform.ToString(), w.EventType.ToString(), w.Processed, w.CreatedAt);

    public static WhatsAppMessageResponse ParaResposta(this WhatsAppMessage m) => new(
        m.Id,
        m.PhoneNumber,
        m.Body,
        m.Status.ToString(),
        // Cliente pode ter sido removido: o historico sobrevive sem o vinculo.
        m.Customer?.Name ?? string.Empty,
        m.CreatedAt);

    public static List<TResposta> ParaRespostas<TEntidade, TResposta>(
        this IEnumerable<TEntidade> itens, Func<TEntidade, TResposta> converter) =>
        itens.Select(converter).ToList();
}

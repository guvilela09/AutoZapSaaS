using System.Security.Cryptography;
using System.Text;
using AutoZapSaaS.Application.Services.Implementations;
using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Tests;

public class WebhookSignatureValidatorTests
{
    private readonly WebhookSignatureValidator _validator = new();

    private const string Secret = "segredo-compartilhado-com-a-plataforma";
    private const string Body = """{"customer_name":"Fulano","customer_phone":"5511999999999"}""";

    [Fact]
    public void Nuvemshop_aceita_assinatura_correta()
    {
        var assinatura = HmacHex(new HMACSHA256(Encoding.UTF8.GetBytes(Secret)), Body);

        Assert.True(_validator.IsValid(WebhookPlatform.Nuvemshop, Body, assinatura, Secret));
    }

    [Fact]
    public void Nuvemshop_rejeita_corpo_adulterado()
    {
        // Cenário real: atacante intercepta um webhook válido e troca o telefone.
        var assinatura = HmacHex(new HMACSHA256(Encoding.UTF8.GetBytes(Secret)), Body);
        var corpoAdulterado = Body.Replace("5511999999999", "5511000000000");

        Assert.False(_validator.IsValid(WebhookPlatform.Nuvemshop, corpoAdulterado, assinatura, Secret));
    }

    [Fact]
    public void Kiwify_aceita_assinatura_correta()
    {
        var assinatura = HmacHex(new HMACSHA1(Encoding.UTF8.GetBytes(Secret)), Body);

        Assert.True(_validator.IsValid(WebhookPlatform.Kiwify, Body, assinatura, Secret));
    }

    [Fact]
    public void Hotmart_compara_o_hottok()
    {
        Assert.True(_validator.IsValid(WebhookPlatform.Hotmart, Body, Secret, Secret));
        Assert.False(_validator.IsValid(WebhookPlatform.Hotmart, Body, "hottok-errado", Secret));
    }

    [Fact]
    public void Rejeita_quando_nao_ha_assinatura()
    {
        Assert.False(_validator.IsValid(WebhookPlatform.Nuvemshop, Body, null, Secret));
        Assert.False(_validator.IsValid(WebhookPlatform.Nuvemshop, Body, "", Secret));
    }

    [Fact]
    public void Rejeita_assinatura_gerada_com_outro_secret()
    {
        var assinatura = HmacHex(new HMACSHA256(Encoding.UTF8.GetBytes("secret-do-atacante")), Body);

        Assert.False(_validator.IsValid(WebhookPlatform.Nuvemshop, Body, assinatura, Secret));
    }

    private static string HmacHex(HMAC hmac, string body)
    {
        using (hmac)
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}

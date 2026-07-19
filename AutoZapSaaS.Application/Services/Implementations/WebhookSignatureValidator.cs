using System.Security.Cryptography;
using System.Text;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Domain.Enums;

namespace AutoZapSaaS.Application.Services.Implementations;

/// <summary>
/// Cada plataforma assina de um jeito diferente. Sem essa validação, qualquer um que
/// descubra a URL do webhook consegue disparar mensagens no nome do tenant.
/// </summary>
public class WebhookSignatureValidator : IWebhookSignatureValidator
{
    public bool IsValid(WebhookPlatform platform, string rawBody, string? signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(secret))
            return false;

        return platform switch
        {
            // Kiwify: ?signature=<hex> — HMAC-SHA1 do corpo cru.
            WebhookPlatform.Kiwify =>
                FixedTimeEquals(signature, ToHex(HmacSha1(rawBody, secret))),

            // Nuvemshop: header x-linkedstore-hmac-sha256 — HMAC-SHA256 do corpo cru.
            WebhookPlatform.Nuvemshop =>
                FixedTimeEquals(signature, ToHex(HmacSha256(rawBody, secret))),

            // Hotmart: header X-HOTMART-HOTTOK — token estático, comparado diretamente.
            WebhookPlatform.Hotmart =>
                FixedTimeEquals(signature, secret),

            _ => false
        };
    }

    private static byte[] HmacSha1(string body, string secret)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
    }

    private static byte[] HmacSha256(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    /// <summary>
    /// Comparação em tempo constante: comparar com == vazaria o segredo por timing.
    /// </summary>
    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a.Trim());
        var bytesB = Encoding.UTF8.GetBytes(b.Trim());

        return bytesA.Length == bytesB.Length
            && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}

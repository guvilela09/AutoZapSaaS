namespace AutoZapSaaS.Application.Common;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AutoZapSaaS";
    public string Audience { get; set; } = "AutoZapSaaS";
    public int ExpirationMinutes { get; set; } = 1440;
}

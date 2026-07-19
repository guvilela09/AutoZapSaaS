namespace AutoZapSaaS.Application.Common;

public class AppSettings
{
    /// <summary>
    /// URL pública da API, usada para montar a URL de webhook que o tenant cadastra
    /// na plataforma de vendas. Ex.: https://api.autozap.com.br
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}

using System.Security.Claims;
using AutoZapSaaS.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AutoZapSaaS.Web.Services;

public static class SessaoExtensions
{
    /// <summary>
    /// Abre a sessao guardando o JWT da API como claim dentro do cookie.
    /// O cookie e HttpOnly e criptografado pelo Data Protection, entao o token
    /// nao fica exposto ao navegador como ficaria em localStorage.
    /// </summary>
    public static async Task AbrirSessaoAsync(this HttpContext contexto, LoginResponse login)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, login.Email),
            new(ClaimTypes.Role, login.Role),
            new("tenant_id", login.TenantId.ToString()),
            new("jwt", login.Token)
        };

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await contexto.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade));
    }

    public static Task FecharSessaoAsync(this HttpContext contexto) =>
        contexto.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

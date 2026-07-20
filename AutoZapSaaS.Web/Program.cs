using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// A API e obrigatoria: sem ela o painel nao faz nada, entao falha no startup
// em vez de mostrar erro em toda tela.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "Api:BaseUrl nao configurada. Defina a variavel de ambiente Api__BaseUrl " +
        "apontando para a AutoZap API (ex.: http://localhost:5000).");
}

// Exige login em tudo por padrao. Liberar exige [AllowAnonymous] explicito,
// entao esquecer um [Authorize] deixa de expor tela.
var exigirLogin = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(exigirLogin));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<AutoZapApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Sair";
        options.AccessDeniedPath = "/Conta/Login";

        // O JWT da API vive dentro do cookie, que e HttpOnly e criptografado pelo
        // Data Protection: nao fica acessivel a JavaScript, ao contrario de
        // guardar o token em localStorage.
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        // Alinhado com a expiracao do JWT emitido pela API (1440 min).
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = false;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Painel/Erro");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Painel}/{action=Index}/{id?}");

app.Run();

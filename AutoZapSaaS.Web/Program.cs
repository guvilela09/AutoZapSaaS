using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

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

builder.Services.AddRazorPages(options =>
{
    // Tudo exige login, menos o que estiver explicitamente liberado.
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Cadastro");
    options.Conventions.AllowAnonymousToPage("/Error");
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
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";

        // O JWT da API vive dentro do cookie, que e HttpOnly: nao fica acessivel
        // a JavaScript, ao contrario de guardar em localStorage.
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
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

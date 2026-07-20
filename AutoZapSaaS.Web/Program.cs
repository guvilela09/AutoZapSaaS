using AutoZapSaaS.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

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
    // Exige login em tudo; liberar exige excecao explicita aqui. Esquecer um
    // [Authorize] numa pagina nova nao expoe tela.
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Cadastro");
    options.Conventions.AllowAnonymousToPage("/Erro");
})
.AddMvcOptions(options =>
{
    // Antiforgery em todo POST, sem depender de lembrar do atributo pagina a pagina.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
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
    app.UseExceptionHandler("/Erro");
    app.UseHsts();
}

// Cabecalhos de defesa no navegador. O painel nao carrega nada de terceiros,
// entao a CSP pode ser restritiva sem quebrar tela.
app.Use(async (contexto, proximo) =>
{
    var cabecalhos = contexto.Response.Headers;
    cabecalhos["X-Content-Type-Options"] = "nosniff";
    cabecalhos["X-Frame-Options"] = "DENY";
    cabecalhos["Referrer-Policy"] = "strict-origin-when-cross-origin";
    cabecalhos["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +      // QR Code chega como data URI
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    await proximo();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

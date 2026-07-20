using System.Text;
using System.Threading.RateLimiting;
using AutoZapSaaS.API.Common;
using AutoZapSaaS.Application;
using AutoZapSaaS.Application.Common.Interfaces;
using AutoZapSaaS.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AutoZap SaaS API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

var jwtSettings = builder.Configuration.GetSection("Jwt");

// Sem fallback: um segredo default em produção significa que qualquer um que leia o
// repositório consegue forjar um JWT de qualquer tenant. Melhor não subir.
var secret = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret não configurado ou menor que 32 caracteres. " +
        "Defina a variável de ambiente Jwt__Secret (veja .env.example).");
}

if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection não configurada. " +
        "Defina a variável de ambiente ConnectionStrings__DefaultConnection.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "AutoZapSaaS",
        ValidAudience = jwtSettings["Audience"] ?? "AutoZapSaaS",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),

        // O padrao do .NET aceita 5 minutos de folga apos o vencimento. Num SaaS
        // onde suspender conta e como se corta acesso, esse atraso importa.
        ClockSkew = TimeSpan.FromSeconds(30),

        // Impede que um token assinado com outro algoritmo seja aceito.
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
    };
});

// Origens liberadas vem da configuracao. AllowAnyOrigin deixava qualquer site
// chamar a API; com credencial em header isso nao e CSRF, mas amplia a superficie
// a toa e reprova em qualquer auditoria.
var origensPermitidas = builder.Configuration
    .GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (origensPermitidas.Length > 0)
        {
            policy.WithOrigins(origensPermitidas)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Sem origens configuradas, nenhum navegador de terceiro passa.
            // O painel server-side nao depende de CORS para funcionar.
            policy.WithOrigins(Array.Empty<string>());
        }
    });
});

// Freio de forca bruta no login. Sem isso, tentar milhares de senhas por minuto
// contra /api/auth/login nao encontra resistencia nenhuma.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("autenticacao", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Teto geral por IP, para nao virar canal de abuso do restante da API.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoZap SaaS API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

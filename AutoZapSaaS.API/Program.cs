using System.Text;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

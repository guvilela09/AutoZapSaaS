using AutoZapSaaS.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoZapSaaS.Infrastructure.Persistence;

/// <summary>
/// Usada apenas pelo `dotnet ef` em tempo de design. O ApplicationDbContext exige um
/// ITenantContext (para os query filters) que não existe fora de uma requisição HTTP.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Só gera/aplica migrations — não precisa dos segredos da aplicação.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=AutoZapSaaS_Db;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, b => b.MigrationsAssembly("AutoZapSaaS.Infrastructure"))
            .Options;

        return new ApplicationDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;
        public bool IsSet => false;
        public void SetTenant(Guid tenantId) => throw new NotSupportedException();
    }
}

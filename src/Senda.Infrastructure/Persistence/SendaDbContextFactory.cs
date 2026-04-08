using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Senda.Core.Interfaces;

namespace Senda.Infrastructure.Persistence;

public class SendaDbContextFactory : IDesignTimeDbContextFactory<SendaDbContext>
{
    public SendaDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SendaDbContext>();
        
        // This is only for migrations, so we can use a dummy connection string or read from appsettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Senda.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=localhost;Database=senda_db;Username=admin;Password=senda_secure_pass";

        builder.UseNpgsql(connectionString, o => o.UseVector());

        return new SendaDbContext(builder.Options, new DesignTimeTenantContext());
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public void SetTenantId(Guid tenantId) { }
    }
}

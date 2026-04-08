using Microsoft.EntityFrameworkCore;
using Senda.Core.Entities;
using Senda.Core.Repositories;
using Senda.Infrastructure.Persistence;

namespace Senda.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly SendaDbContext _context;

    public TenantRepository(SendaDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        return await _context.Tenants.FindAsync(id);
    }

    public async Task<Tenant?> GetActiveTenantByIdAsync(Guid id)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
    }

    public async Task<IEnumerable<Tenant>> GetAllActiveAsync()
    {
        return await _context.Tenants
            .Where(t => t.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(Tenant tenant)
    {
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
    }
}

using Senda.Core.Entities;

namespace Senda.Core.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id);
    Task<Tenant?> GetActiveTenantByIdAsync(Guid id);
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task AddAsync(Tenant tenant);
    Task UpdateAsync(Tenant tenant);
    Task DeleteAsync(Guid id);
}

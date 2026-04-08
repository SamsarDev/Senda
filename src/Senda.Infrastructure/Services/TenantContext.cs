using Senda.Core.Interfaces;

namespace Senda.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}

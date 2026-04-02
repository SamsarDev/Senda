namespace Senda.Core.Exceptions;

public class TenantNotFoundException : Exception
{
    public Guid TenantId { get; }

    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant with ID '{tenantId}' was not found.")
    {
        TenantId = tenantId;
    }
}

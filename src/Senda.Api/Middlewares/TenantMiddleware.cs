using Senda.Core.Interfaces;

namespace Senda.Api.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private const string TenantHeader = "X-Tenant-Id";

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                tenantContext.SetTenantId(tenantId);
            }
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}

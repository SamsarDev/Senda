using Microsoft.AspNetCore.Mvc;
using Senda.Core.Entities;
using Senda.Core.Repositories;

namespace Senda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepository _tenantRepository;

    public TenantsController(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true
        };

        await _tenantRepository.AddAsync(tenant);
        return Ok(tenant);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _tenantRepository.GetAllAsync();
        return Ok(tenants);
    }
}

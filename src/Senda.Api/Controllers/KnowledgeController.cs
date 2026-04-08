using MediatR;
using Microsoft.AspNetCore.Mvc;
using Senda.Application.Commands;
using Senda.Core.Interfaces;
using Senda.Core.Repositories;

namespace Senda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;
    private readonly IKnowledgeRepository _knowledgeRepository;

    public KnowledgeController(IMediator mediator, ITenantContext tenantContext, IKnowledgeRepository knowledgeRepository)
    {
        _mediator = mediator;
        _tenantContext = tenantContext;
        _knowledgeRepository = knowledgeRepository;
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized("X-Tenant-Id header is required.");

        var documents = await _knowledgeRepository.GetDocumentsByTenantAsync(_tenantContext.TenantId.Value);
        return Ok(documents);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (_tenantContext.TenantId == null)
            return Unauthorized("X-Tenant-Id header is required.");

        using var stream = file.OpenReadStream();
        var command = new IngestDocumentCommand
        {
            TenantId = _tenantContext.TenantId.Value,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileStream = stream
        };

        var documentId = await _mediator.Send(command);
        return Ok(new { DocumentId = documentId });
    }
}

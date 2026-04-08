using MediatR;
using Microsoft.AspNetCore.Mvc;
using Senda.Application.Commands;
using Senda.Core.Interfaces;

namespace Senda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public ChatController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator = mediator;
        _tenantContext = tenantContext;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (_tenantContext.TenantId == null)
            return Unauthorized("X-Tenant-Id header is required.");

        var command = new SendMessageCommand
        {
            TenantId = _tenantContext.TenantId.Value,
            SessionId = request.SessionId,
            CustomerIdentifier = request.CustomerIdentifier,
            Message = request.Message
        };

        var response = await _mediator.Send(command);
        return Ok(response);
    }
}

public record SendMessageRequest(Guid? SessionId, string CustomerIdentifier, string Message);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalR.Api.Auth;
using SignalR.BLL;
using SignalR.BLL.Approvals;

namespace SignalR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvals;

    public ApprovalsController(IApprovalService approvals)
    {
        _approvals = approvals;
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Requester)]
    public async Task<ActionResult<IReadOnlyList<ApprovalDto>>> Mine(CancellationToken cancellationToken)
    {
        var items = await _approvals.GetMineAsync(User.GetUserId(), cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Requester)]
    public async Task<ActionResult<ApprovalDto>> Create(CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var created = await _approvals.CreateAsync(User.GetUserId(), request.Title, request.Details, cancellationToken);
        return Created($"/api/approvals/{created.Id}", created);
    }

    [HttpGet("pending")]
    [Authorize(Roles = AppRoles.Approver)]
    public async Task<ActionResult<IReadOnlyList<ApprovalDto>>> Pending(CancellationToken cancellationToken)
    {
        var items = await _approvals.GetPendingAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = AppRoles.Approver)]
    public async Task<ActionResult<ApprovalDto>> Decide(Guid id, DecideApprovalRequest request, CancellationToken cancellationToken)
    {
        var result = await _approvals.DecideAsync(User.GetUserId(), id, request.Approved, request.Note, cancellationToken);
        return result.Outcome switch
        {
            DecideOutcome.NotFound => NotFound(new { message = "Approval request was not found." }),
            DecideOutcome.NotPending => Conflict(new { message = "This request has already been decided." }),
            _ => Ok(result.Approval)
        };
    }
}

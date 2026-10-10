using System.ComponentModel.DataAnnotations;

namespace SignalR.BLL.Approvals;

public sealed class CreateApprovalRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(2000)]
    public string Details { get; set; } = "";
}

public sealed class DecideApprovalRequest
{
    public bool Approved { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

public sealed record ApprovalDto(
    Guid Id,
    string Title,
    string Details,
    Guid RequesterId,
    string RequesterName,
    string Status,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime? DecidedAt);

public enum DecideOutcome
{
    Updated,
    NotFound,
    NotPending
}

public sealed record DecideResult(DecideOutcome Outcome, ApprovalDto? Approval);

public interface IApprovalService
{
    Task<ApprovalDto> CreateAsync(Guid requesterId, string title, string details, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovalDto>> GetMineAsync(Guid requesterId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ApprovalDto>> GetPendingAsync(CancellationToken cancellationToken);

    Task<DecideResult> DecideAsync(Guid approverId, Guid requestId, bool approved, string? note, CancellationToken cancellationToken);
}

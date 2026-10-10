using Microsoft.EntityFrameworkCore;
using SignalR.BLL.Realtime;
using SignalR.DAL.Data;
using SignalR.DAL.Entities;

namespace SignalR.BLL.Approvals;

public sealed class ApprovalService : IApprovalService
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotifier _notifier;

    public ApprovalService(AppDbContext db, IRealtimeNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<ApprovalDto> CreateAsync(Guid requesterId, string title, string details, CancellationToken cancellationToken)
    {
        var requester = await _db.Users
            .AsNoTracking()
            .FirstAsync(user => user.Id == requesterId, cancellationToken);

        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Details = details.Trim(),
            RequesterId = requesterId,
            Status = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(request, requester.DisplayName);
        await _notifier.PushToRoleAsync(AppRoles.Approver, ApprovalEvents.Requested, dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyList<ApprovalDto>> GetMineAsync(Guid requesterId, CancellationToken cancellationToken)
    {
        var requests = await _db.ApprovalRequests
            .AsNoTracking()
            .Include(request => request.Requester)
            .Where(request => request.RequesterId == requesterId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(request => ToDto(request, request.Requester.DisplayName)).ToArray();
    }

    public async Task<IReadOnlyList<ApprovalDto>> GetPendingAsync(CancellationToken cancellationToken)
    {
        var requests = await _db.ApprovalRequests
            .AsNoTracking()
            .Include(request => request.Requester)
            .Where(request => request.Status == ApprovalStatus.Pending)
            .OrderBy(request => request.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(request => ToDto(request, request.Requester.DisplayName)).ToArray();
    }

    public async Task<DecideResult> DecideAsync(Guid approverId, Guid requestId, bool approved, string? note, CancellationToken cancellationToken)
    {
        var request = await _db.ApprovalRequests
            .Include(candidate => candidate.Requester)
            .FirstOrDefaultAsync(candidate => candidate.Id == requestId, cancellationToken);

        if (request is null)
        {
            return new DecideResult(DecideOutcome.NotFound, null);
        }

        if (request.Status != ApprovalStatus.Pending)
        {
            return new DecideResult(DecideOutcome.NotPending, ToDto(request, request.Requester.DisplayName));
        }

        request.Status = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        request.DecidedById = approverId;
        request.DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        request.DecidedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(request, request.Requester.DisplayName);
        await _notifier.PushToUserAsync(request.RequesterId, ApprovalEvents.Decided, dto, cancellationToken);
        return new DecideResult(DecideOutcome.Updated, dto);
    }

    private static ApprovalDto ToDto(ApprovalRequest request, string requesterName) =>
        new(
            request.Id,
            request.Title,
            request.Details,
            request.RequesterId,
            requesterName,
            request.Status.ToString(),
            request.DecisionNote,
            request.CreatedAt,
            request.DecidedAt);
}

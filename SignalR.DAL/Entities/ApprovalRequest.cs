namespace SignalR.DAL.Entities;

public sealed class ApprovalRequest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = "";

    public string Details { get; set; } = "";

    public Guid RequesterId { get; set; }

    public User Requester { get; set; } = null!;

    public ApprovalStatus Status { get; set; }

    public Guid? DecidedById { get; set; }

    public User? DecidedBy { get; set; }

    public string? DecisionNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DecidedAt { get; set; }
}

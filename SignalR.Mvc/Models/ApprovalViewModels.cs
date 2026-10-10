using System.ComponentModel.DataAnnotations;

namespace SignalR.Mvc.Models;

public sealed class LoginViewModel
{
    [Required]
    public string UserName { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public sealed class CreateApprovalViewModel
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required]
    [MaxLength(2000)]
    public string Details { get; set; } = "";
}

public sealed class DecideApprovalViewModel
{
    public Guid Id { get; set; }

    public bool Approved { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}

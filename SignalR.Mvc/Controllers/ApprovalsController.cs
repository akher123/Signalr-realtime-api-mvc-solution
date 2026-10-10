using Microsoft.AspNetCore.Mvc;
using SignalR.Mvc.Models;
using SignalR.Mvc.Services;

namespace SignalR.Mvc.Controllers;

public sealed class ApprovalsController : Controller
{
    private readonly ApprovalApiClient _api;

    public ApprovalsController(ApprovalApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        if (RequireRole(SessionKeys.Requester) is { } redirect)
        {
            return redirect;
        }

        ViewData["SignalRPage"] = "mine";
        var result = await _api.GetMineAsync(Token()!, cancellationToken);
        if (RedirectForAuth(result.StatusCode) is { } authRedirect)
        {
            return authRedirect;
        }

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }

        return View(result.Value ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateApprovalViewModel model, CancellationToken cancellationToken)
    {
        if (RequireRole(SessionKeys.Requester) is { } redirect)
        {
            return redirect;
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a title and details before submitting.";
            return RedirectToAction(nameof(Mine));
        }

        var result = await _api.CreateAsync(Token()!, model.Title, model.Details, cancellationToken);
        if (RedirectForAuth(result.StatusCode) is { } authRedirect)
        {
            return authRedirect;
        }

        TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
            ? "Request submitted. Approvers are being notified."
            : result.Error;
        return RedirectToAction(nameof(Mine));
    }

    [HttpGet]
    public async Task<IActionResult> Inbox(CancellationToken cancellationToken)
    {
        if (RequireRole(SessionKeys.Approver) is { } redirect)
        {
            return redirect;
        }

        ViewData["SignalRPage"] = "inbox";
        var result = await _api.GetPendingAsync(Token()!, cancellationToken);
        if (RedirectForAuth(result.StatusCode) is { } authRedirect)
        {
            return authRedirect;
        }

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error;
        }

        return View(result.Value ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(DecideApprovalViewModel model, CancellationToken cancellationToken)
    {
        if (RequireRole(SessionKeys.Approver) is { } redirect)
        {
            return redirect;
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The decision could not be saved.";
            return RedirectToAction(nameof(Inbox));
        }

        var result = await _api.DecideAsync(Token()!, model.Id, model.Approved, model.Note, cancellationToken);
        if (RedirectForAuth(result.StatusCode) is { } authRedirect)
        {
            return authRedirect;
        }

        TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
            ? "Decision saved. The requester has been notified."
            : result.Error;
        return RedirectToAction(nameof(Inbox));
    }

    private string? Token() => HttpContext.Session.GetString(SessionKeys.Token);

    private IActionResult? RequireRole(string role)
    {
        if (Token() is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var roles = HttpContext.Session.GetString(SessionKeys.Roles) ?? "";
        if (!roles.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(role))
        {
            TempData["Error"] = $"That page requires the {role} role.";
            return RedirectToAction("Index", "Home");
        }

        return null;
    }

    private IActionResult? RedirectForAuth(int statusCode)
    {
        if (statusCode != StatusCodes.Status401Unauthorized)
        {
            return null;
        }

        HttpContext.Session.Clear();
        TempData["Error"] = "Your sign-in expired. Please sign in again.";
        return RedirectToAction("Login", "Account");
    }
}

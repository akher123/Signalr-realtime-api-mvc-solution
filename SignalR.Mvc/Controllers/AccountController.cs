using Microsoft.AspNetCore.Mvc;
using SignalR.Mvc.Models;
using SignalR.Mvc.Services;

namespace SignalR.Mvc.Controllers;

public sealed class AccountController : Controller
{
    private readonly ApprovalApiClient _api;

    public AccountController(ApprovalApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString(SessionKeys.Token) is not null)
        {
            return RedirectAfterLogin(CurrentRoles());
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.LoginAsync(model.UserName, model.Password, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Sign-in failed.");
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.Token, result.Value.Token);
        HttpContext.Session.SetString(SessionKeys.DisplayName, result.Value.DisplayName);
        HttpContext.Session.SetString(SessionKeys.Roles, string.Join(',', result.Value.Roles));
        return RedirectAfterLogin(result.Value.Roles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectAfterLogin(IReadOnlyList<string> roles)
    {
        if (roles.Contains(SessionKeys.Requester))
        {
            return RedirectToAction("Mine", "Approvals");
        }

        if (roles.Contains(SessionKeys.Approver))
        {
            return RedirectToAction("Inbox", "Approvals");
        }

        return RedirectToAction("Index", "Home");
    }

    private IReadOnlyList<string> CurrentRoles() =>
        (HttpContext.Session.GetString(SessionKeys.Roles) ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries);
}

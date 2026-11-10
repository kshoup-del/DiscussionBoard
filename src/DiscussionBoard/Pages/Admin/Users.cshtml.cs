using DiscussionBoard.Authorization;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public class UsersModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationAuthorizationService _authService;

    public UsersModel(
        UserManager<ApplicationUser> userManager,
        IApplicationAuthorizationService authService)
    {
        _userManager = userManager;
        _authService = authService;
    }

    [BindProperty] public string? SelectedUserId { get; set; }
    [BindProperty] public string? RoleName { get; set; }
    [BindProperty] public string? Operation { get; set; } // "add" or "remove"

    public List<SelectListItem> UserOptions { get; private set; } = new();
    public ApplicationUser? SelectedUser { get; private set; }
    public bool SelectedUserIsAdmin { get; private set; }
    public string? Message { get; private set; }
    public bool MessageIsSuccess { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadUsersAsync();
    }

    // Form 1: choose a user
    public async Task<IActionResult> OnPostAsync()
    {
        await LoadUsersAsync();

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            SetMessage(false, "Please select a user.");
            return Page();
        }

        if (!await LoadSelectedUserAsync())
        {
            SetMessage(false, "The selected user does not exist.");
            SelectedUserId = null;
        }

        return Page();
    }

    // Form 2: add or remove the Admin role
    public async Task<IActionResult> OnPostUpdateRoleAsync()
    {
        await LoadUsersAsync();

        if (string.IsNullOrWhiteSpace(SelectedUserId) || !await LoadSelectedUserAsync())
        {
            SetMessage(false, "The selected user does not exist.");
            SelectedUserId = null;
            return Page();
        }

        if (RoleName != ApplicationRoles.Admin)
        {
            SetMessage(false, "That role is not supported.");
            return Page();
        }

        IdentityResult result;
        switch (Operation)
        {
            case "add":
                result = await _authService.AddRoleAsync(User, SelectedUserId, RoleName);
                break;
            case "remove":
                result = await _authService.RemoveRoleAsync(User, SelectedUserId, RoleName);
                break;
            default:
                SetMessage(false, "Invalid operation.");
                return Page();
        }

        if (result.Succeeded)
        {
            SetMessage(true, Operation == "add"
                ? $"{SelectedUser!.DisplayName} was added to the {RoleName} role."
                : $"{SelectedUser!.DisplayName} was removed from the {RoleName} role.");
        }
        else
        {
            SetMessage(false, string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await LoadSelectedUserAsync(); // refresh so the correct button is shown
        return Page();
    }

    private async Task LoadUsersAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.DisplayName).ToListAsync();
        UserOptions = users
            .Select(u => new SelectListItem($"{u.DisplayName} ({u.Email})", u.Id))
            .ToList();
    }

    private async Task<bool> LoadSelectedUserAsync()
    {
        SelectedUser = await _userManager.FindByIdAsync(SelectedUserId!);
        if (SelectedUser is null) return false;

        SelectedUserIsAdmin = await _userManager.IsInRoleAsync(SelectedUser, ApplicationRoles.Admin);
        return true;
    }

    private void SetMessage(bool success, string text)
    {
        MessageIsSuccess = success;
        Message = text;
    }
}

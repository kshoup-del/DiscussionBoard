using System.Security.Claims;
using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Authorization;

public class ApplicationAuthorizationService : IApplicationAuthorizationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthorizationService _authorizationService;

    public ApplicationAuthorizationService(
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        _userManager = userManager;
        _authorizationService = authorizationService;
    }

    public async Task<bool> CanEditAsync(ClaimsPrincipal principal, DiscussionBase resource)
    {
        var result = await _authorizationService.AuthorizeAsync(
            principal, resource, ApplicationAuthorizationPolicies.IsOwnerOrAdmin);
        return result.Succeeded;
    }

    public async Task<IdentityResult> AddRoleAsync(
        ClaimsPrincipal actingUser, string targetUserId, string roleName)
    {
        var denied = await VerifyAdminActorAsync(actingUser);
        if (denied is not null) return denied;

        if (roleName != ApplicationRoles.Admin)
            return Fail("InvalidRole", "That role is not supported.");

        var target = await _userManager.FindByIdAsync(targetUserId);
        if (target is null)
            return Fail("UserNotFound", "The selected user does not exist.");

        if (await _userManager.IsInRoleAsync(target, roleName))
            return Fail("AlreadyInRole", "The user is already in that role.");

        return await _userManager.AddToRoleAsync(target, roleName);
    }

    public async Task<IdentityResult> RemoveRoleAsync(
        ClaimsPrincipal actingUser, string targetUserId, string roleName)
    {
        var denied = await VerifyAdminActorAsync(actingUser);
        if (denied is not null) return denied;

        if (roleName != ApplicationRoles.Admin)
            return Fail("InvalidRole", "That role is not supported.");

        if (targetUserId == _userManager.GetUserId(actingUser))
            return Fail("SelfRemoval", "You cannot remove your own Admin role.");

        var target = await _userManager.FindByIdAsync(targetUserId);
        if (target is null)
            return Fail("UserNotFound", "The selected user does not exist.");

        if (!await _userManager.IsInRoleAsync(target, roleName))
            return Fail("NotInRole", "The user is not in that role.");

        var admins = await _userManager.GetUsersInRoleAsync(ApplicationRoles.Admin);
        if (admins.Count <= 1)
            return Fail("LastAdmin", "You cannot remove the last remaining Admin.");

        return await _userManager.RemoveFromRoleAsync(target, roleName);
    }

    private async Task<IdentityResult?> VerifyAdminActorAsync(ClaimsPrincipal actor)
    {
        if (actor.Identity?.IsAuthenticated != true)
            return Fail("NotAuthenticated", "You must be signed in.");

        var actorUser = await _userManager.GetUserAsync(actor);
        if (actorUser is null || !await _userManager.IsInRoleAsync(actorUser, ApplicationRoles.Admin))
            return Fail("NotAdmin", "Only administrators can change roles.");

        return null;
    }

    private static IdentityResult Fail(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}

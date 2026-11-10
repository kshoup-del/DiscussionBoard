using DiscussionBoard.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DiscussionBoard.Authorization
{
    public class IsOwnerOrAdminHandler : AuthorizationHandler<IsOwnerOrAdminRequirement, DiscussionBase>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOwnerOrAdminRequirement requirement,
            DiscussionBase resource)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            if (context.User.IsInRole(ApplicationRoles.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && resource.ApplicationUserId == userId)
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}

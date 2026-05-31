using System.Security.Claims;
using API.Security.Authorization.Requirements;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace API.Security.Authorization.Handlers;

/// <summary>
/// Authorization handler that enforces the <see cref="ResourceOwnerRequirement"/>,
/// ensuring the authenticated user is the owner of the resource being accessed.
/// </summary>
/// <remarks>
/// The handler follows a three-step evaluation:
/// <list type="number">
///   <item><description>
///     <b>Admin bypass</b> — users in the <c>Admin</c> role are granted access
///     unconditionally, allowing administrators to manage any resource.
///   </description></item>
///   <item><description>
///     <b>Identity check</b> — if either the authenticated user ID or the resource
///     owner ID cannot be determined, the requirement fails with an explicit reason
///     rather than silently denying access.
///   </description></item>
///   <item><description>
///     <b>Ownership check</b> — access is granted only when the authenticated user ID
///     matches the resource owner ID; any mismatch is logged and fails the requirement.
///   </description></item>
/// </list>
/// The resource owner ID is expected to be passed as a <see cref="string"/> via
/// <c>IAuthorizationService.AuthorizeAsync</c> from the controller.
/// </remarks>
/// <param name="logger">The logger used to record warnings for missing IDs and ownership violations.</param>
public sealed class ResourceOwnerHandler(ILogger<ResourceOwnerHandler> logger)
    : AuthorizationHandler<ResourceOwnerRequirement>
{
    /// <summary>
    /// Evaluates the <see cref="ResourceOwnerRequirement"/> against the current user and resource.
    /// </summary>
    /// <param name="context">
    /// The authorization handler context, providing the user principal and the resource object.
    /// </param>
    /// <param name="requirement">The requirement being evaluated.</param>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceOwnerRequirement requirement
    )
    {
        // Extract the stable user ID from the NameIdentifier claim; this is the same
        // claim used throughout the application to identify the authenticated user.
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ── Admin bypass ──────────────────────────────────────
        // Administrators are permitted to access any resource regardless of ownership,
        // supporting administrative use cases such as support and moderation tooling.
        if (context.User.IsInRole(UserRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // ── Resource owner resolution ─────────────────────────
        // The controller passes the resource owner's user ID as a plain string via
        // IAuthorizationService.AuthorizeAsync(user, ownerId, policy). If the cast
        // fails (wrong type or null resource), the ownership check cannot proceed.
        var resourceUserId = context.Resource as string;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(resourceUserId))
        {
            // Log a warning rather than silently failing so misconfigured call sites
            // (e.g. controller not passing the resource) are easy to diagnose.
            logger.LogWarning("ResourceOwnerHandler: missing userId or resourceUserId.");

            var reason = new AuthorizationFailureReason(this, "Resource ownership could not be determined.");
            context.Fail(reason);

            return Task.CompletedTask;
        }

        // ── Ownership check ───────────────────────────────────
        if (userId == resourceUserId)
        {
            // The authenticated user is the owner — grant access.
            context.Succeed(requirement);
        }
        else
        {
            // Log the attempted cross-user access so it is visible in security audits
            // and structured log dashboards without requiring a separate audit table.
            logger.LogWarning(
                "ResourceOwnerHandler: user {UserId} attempted to access " +
                "resource owned by {OwnerId}.",
                userId, resourceUserId);

            var reason = new AuthorizationFailureReason(this, "You do not own this resource.");
            context.Fail(reason);
        }

        return Task.CompletedTask;
    }
}
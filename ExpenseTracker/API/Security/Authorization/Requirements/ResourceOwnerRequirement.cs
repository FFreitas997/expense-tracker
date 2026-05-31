using Microsoft.AspNetCore.Authorization;

namespace API.Security.Authorization.Requirements;

/// <summary>
/// Marker requirement that enforces resource ownership as an authorization policy.
/// </summary>
/// <remarks>
/// This class carries no configuration state — all evaluation logic resides in
/// <c>ResourceOwnerHandler</c>, which grants access only when the authenticated
/// user's ID matches the resource owner ID passed by the controller.
/// <para>
/// Register the policy in the authorization configuration and apply it to
/// controller actions via <c>[Authorize(Policy = "...")]</c>, then pass the
/// resource owner ID to <c>IAuthorizationService.AuthorizeAsync</c> at the
/// call site so the handler can perform the ownership comparison.
/// </para>
/// </remarks>
public sealed class ResourceOwnerRequirement : IAuthorizationRequirement
{
    // Marker — all evaluation logic is in ResourceOwnerHandler.
}
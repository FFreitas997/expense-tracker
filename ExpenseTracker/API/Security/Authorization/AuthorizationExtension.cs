using API.Security.Authorization.Handlers;
using API.Security.Authorization.Policies;
using API.Security.Authorization.Requirements;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace API.Security.Authorization;

/// <summary>
/// Extension methods for registering authorization handlers and policies
/// on the <see cref="IServiceCollection"/>.
/// </summary>
public static class AuthorizationExtension
{
    /// <summary>
    /// Registers the application's authorization handlers and named policies.
    /// </summary>
    /// <remarks>
    /// Four policies are defined:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>AdminOnly</b> — restricts access to users in the <c>Admin</c> role.
    ///   </description></item>
    ///   <item><description>
    ///     <b>MemberOnly</b> — restricts access to users in the <c>Member</c> role.
    ///   </description></item>
    ///   <item><description>
    ///     <b>BackOffice</b> — restricts access to <c>Admin</c> users who also carry
    ///     a <c>backoffice-access: true</c> claim, providing an additional access gate
    ///     beyond role membership alone.
    ///   </description></item>
    ///   <item><description>
    ///     <b>ResourceOwner</b> — delegates evaluation to <see cref="ResourceOwnerHandler"/>,
    ///     granting access only when the authenticated user owns the resource being accessed
    ///     (Admins are bypassed unconditionally).
    ///   </description></item>
    /// </list>
    /// Apply policies to controllers or actions via
    /// <c>[Authorize(Policy = PolicyNames.XYZ)]</c>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <returns>The same <paramref name="services"/> instance to allow method chaining.</returns>
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        // Register ResourceOwnerHandler as scoped so it can access scoped DI services
        // (e.g. repositories) when evaluating the ownership requirement per request.
        services.AddScoped<IAuthorizationHandler, ResourceOwnerHandler>();

        services.AddAuthorizationBuilder()

            // ── AdminOnly ─────────────────────────────────────
            // Grants access exclusively to authenticated users in the Admin role.
            .AddPolicy(PolicyNames.AdminOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Admin))

            // ── MemberOnly ────────────────────────────────────
            // Grants access exclusively to authenticated users in the Member role.
            .AddPolicy(PolicyNames.MemberOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Member))

            // ── BackOffice ────────────────────────────────────
            // Restricts access to Admin users who also hold the backoffice-access claim.
            // The extra claim acts as a second gate so not every Admin automatically
            // gains back-office access — it must be explicitly granted.
            .AddPolicy(PolicyNames.BackOffice, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(UserRoles.Admin)
                .RequireClaim("backoffice-access", "true"))

            // ── ResourceOwner ─────────────────────────────────
            // Delegates ownership evaluation to ResourceOwnerHandler, which compares
            // the authenticated user's ID against the resource owner ID supplied by
            // the controller via IAuthorizationService.AuthorizeAsync.
            .AddPolicy(PolicyNames.ResourceOwner, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ResourceOwnerRequirement()));

        return services;
    }
}
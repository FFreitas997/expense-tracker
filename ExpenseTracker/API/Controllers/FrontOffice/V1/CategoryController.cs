using System.Diagnostics;
using System.Security.Claims;
using API.Observability.Tracing;
using API.Security.Authorization.Policies;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.FrontOffice.V1;

/// <summary>
///     Front-office endpoints for managing categories from an authenticated member's perspective.
///     System (default) categories are visible to all members. Each member can fully manage
///     their own custom categories; resource-owner authorization is enforced on mutations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frontoffice/categories")]
[Authorize(Policy = PolicyNames.MemberOnly)]
public class CategoryController(ICategoryService service, IAuthorizationService authorization) : ControllerBase
{
    /// <summary>
    ///     Returns all categories visible to the authenticated member —
    ///     both system defaults and the member's own custom categories.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the list of <see cref="CategoryResponseDto" /> on success; problem details on failure.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("FrontOffice.CategoryController.GetAll");

        var result = await service.GetAllAsync(GetCurrentUserId(), ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(Ok,
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Returns a single category by its identifier, provided it is a system category
    ///     or belongs to the authenticated member.
    /// </summary>
    /// <param name="id">The unique identifier of the category to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the <see cref="CategoryResponseDto" /> on success; problem details on failure.</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("FrontOffice.CategoryController.GetById");
        activity?.SetTag("category.id", id);

        var result = await service.GetByIdAsync(id, GetCurrentUserId(), ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(Ok,
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Creates a new custom category scoped to the authenticated member.
    ///     The DTO is validated automatically by the global <c>ValidationFilter</c>.
    /// </summary>
    /// <param name="dto">Payload containing name, icon, and hex color.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     201 Created with the new <see cref="CategoryResponseDto" /> on success;
    ///     problem details on failure (409 Conflict if the name is already in use).
    /// </returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("FrontOffice.CategoryController.Create");

        var result = await service.CreateCustomAsync(dto, GetCurrentUserId(), ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            category => Created(
                $"api/v1/frontoffice/categories/{category.Id}", category),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Updates a custom category identified by <paramref name="id" />.
    ///     The DTO is validated automatically by the global <c>ValidationFilter</c>.
    ///     Enforces resource-owner authorization: only the owning member (or an Admin
    ///     via the bypass in <c>ResourceOwnerHandler</c>) may update the category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to update.</param>
    /// <param name="dto">Payload containing updated name, icon, and hex color.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     200 OK with the updated <see cref="CategoryResponseDto" /> on success;
    ///     403 Forbidden when the resource-owner check fails;
    ///     problem details on failure.
    /// </returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CategoryUpdateDto dto,
        CancellationToken ct
    )
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("FrontOffice.CategoryController.Update");
        activity?.SetTag("category.id", id);

        var userId = GetCurrentUserId();

        // Resolve the resource owner before the update to perform the ownership check.
        // GetByIdAsync also validates that the category is accessible to this user.
        var existing = await service.GetByIdAsync(id, userId, ct);
        if (existing.IsFailure)
            return Problem(
                title: existing.Error.Code,
                detail: existing.Error.Details,
                statusCode: (int)existing.Error.Type);

        // Enforce resource-owner policy: only the owning member (or an Admin) may update.
        // System categories have a null UserId, so this check will deny access to them.
        var authResult = await authorization.AuthorizeAsync(
            User, existing.Value.UserId?.ToString(), PolicyNames.ResourceOwner);
        if (!authResult.Succeeded)
        {
            activity?.SetTag("result.status", "forbidden");
            activity?.SetStatus(ActivityStatusCode.Error, "Resource ownership check failed.");
            return Forbid();
        }

        var result = await service.UpdateCustomAsync(id, dto, userId, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(Ok,
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Deletes a custom category identified by <paramref name="id" />.
    ///     The operation is rejected when the category has linked expenses or is a system default.
    ///     Enforces resource-owner authorization before proceeding.
    /// </summary>
    /// <param name="id">The unique identifier of the category to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     204 No Content on success;
    ///     403 Forbidden when the resource-owner check fails;
    ///     problem details on failure.
    /// </returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("FrontOffice.CategoryController.Delete");
        activity?.SetTag("category.id", id);

        var userId = GetCurrentUserId();

        // Resolve the resource owner before deletion to perform the ownership check.
        var existing = await service.GetByIdAsync(id, userId, ct);
        if (existing.IsFailure)
            return Problem(
                title: existing.Error.Code,
                detail: existing.Error.Details,
                statusCode: (int)existing.Error.Type);

        // System categories have a null UserId — the ResourceOwner check will
        // correctly deny this request, preventing members from deleting defaults.
        var authResult = await authorization.AuthorizeAsync(
            User, existing.Value.UserId?.ToString(), PolicyNames.ResourceOwner);
        if (!authResult.Succeeded)
        {
            activity?.SetTag("result.status", "forbidden");
            activity?.SetStatus(ActivityStatusCode.Error, "Resource ownership check failed.");
            return Forbid();
        }

        var result = await service.DeleteCustomAsync(id, userId, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            _ => NoContent(),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    ///     Extracts and parses the authenticated user's ID from the JWT
    ///     <see cref="ClaimTypes.NameIdentifier" /> claim.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
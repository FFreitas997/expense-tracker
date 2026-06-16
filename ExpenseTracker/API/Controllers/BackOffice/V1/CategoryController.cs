using System.Diagnostics;
using API.Observability.Tracing;
using API.Security.Authorization.Policies;
using Application.DTOs.Category;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.BackOffice.V1;

/// <summary>
///     Back-office endpoints for managing system (default) categories.
///     Restricted to administrators only; all operations are delegated to
///     <see cref="ICategoryService" />, which enforces business rules and cache management.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/backoffice/categories")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class CategoryController(ICategoryService service) : ControllerBase
{
    /// <summary>
    ///     Returns the complete list of system (default) categories.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with a list of <see cref="CategoryResponseDto" /> on success; problem details on failure.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAllSystem(CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("BackOffice.CategoryController.GetAllSystem");

        var result = await service.GetAllSystemAsync(ct);

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
            categories => Ok(categories),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Creates a new system (default) category that is visible to all users.
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
    public async Task<IActionResult> CreateSystem([FromBody] CategoryCreateDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("BackOffice.CategoryController.CreateSystem");

        var result = await service.CreateSystemAsync(dto, ct);

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
                $"api/v1/backoffice/categories/{category.Id}", category),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Updates an existing system category identified by <paramref name="id" />.
    ///     The DTO is validated automatically by the global <c>ValidationFilter</c>.
    /// </summary>
    /// <param name="id">The unique identifier of the system category to update.</param>
    /// <param name="dto">Payload containing updated name, icon, and hex color.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     200 OK with the updated <see cref="CategoryResponseDto" /> on success;
    ///     problem details on failure (404 if not found, 409 if name already in use).
    /// </returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateSystem(
        Guid id, [FromBody] CategoryUpdateDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("BackOffice.CategoryController.UpdateSystem");
        activity?.SetTag("category.id", id);

        var result = await service.UpdateSystemAsync(id, dto, ct);

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
            category => Ok(category),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type));
    }

    /// <summary>
    ///     Deletes a system category identified by <paramref name="id" />.
    ///     The operation is rejected when the category has linked expenses.
    /// </summary>
    /// <param name="id">The unique identifier of the system category to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     204 No Content on success;
    ///     problem details on failure (404 if not found, 409 if linked expenses exist).
    /// </returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> DeleteSystem(Guid id, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance
            .StartActivity("BackOffice.CategoryController.DeleteSystem");
        activity?.SetTag("category.id", id);

        var result = await service.DeleteSystemAsync(id, ct);

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
}

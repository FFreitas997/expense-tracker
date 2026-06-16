using System.Diagnostics;
using API.Observability.Tracing;
using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.FrontOffice.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frontoffice/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
    {
        using var activity = AppActivitySource.Instance.StartActivity("AuthController.Register");

        var result = await service.RegisterAsync(dto, ct);

        if (result.IsFailure)
        {
            var error = result.Error;
            activity?.SetTag("result.status", "failure");
            activity?.SetTag("error.code", error.Code);
            activity?.SetTag("error.details", error.Details);
            activity?.SetStatus(ActivityStatusCode.Error, error.Details);
        }
        else
        {
            activity?.SetTag("result.status", "success");
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        return result.Match<IActionResult>(
            token => Created(string.Empty, token),
            error => Problem(
                title: error.Code,
                detail: error.Details,
                statusCode: (int)error.Type
            ));
    }
}
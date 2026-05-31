using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.FrontOffice.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frontoffice/auth")]
public class AuthController : ControllerBase
{
}
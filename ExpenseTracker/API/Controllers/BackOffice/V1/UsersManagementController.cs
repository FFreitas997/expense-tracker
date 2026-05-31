using API.Security.Authorization.Policies;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.BackOffice.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/backoffice/users")]
[Authorize(Policy = PolicyNames.AdminOnly)]
public class UsersManagementController : ControllerBase
{
}
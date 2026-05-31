using API.Security.Authorization.Policies;
using Application.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.FrontOffice.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/frontoffice/expenses")]
[Authorize(Policy = PolicyNames.MemberOnly)]
public class ExpensesController(IExpenseService service, IAuthorizationService authorization) : ControllerBase
{
}
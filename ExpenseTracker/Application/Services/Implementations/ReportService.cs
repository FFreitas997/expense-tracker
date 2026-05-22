using Application.Services.Interfaces;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class ReportService(
    ILogger<ReportService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IReportService
{
}
using Application.Services.Interfaces;
using Infrastructure.Cache.Interfaces;
using Infrastructure.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class ReportService(
    ILogger<ReportService> logger,
    IUnitOfWork unit,
    ICacheRepository cache
) : IReportService
{
}
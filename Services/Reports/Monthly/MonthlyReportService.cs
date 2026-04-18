using AbsoluteCinema.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Monthly;

public sealed class MonthlyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    [FromKeyedServices("monthlyPayment")] IReportService payment,
    [FromKeyedServices("monthlyGross")] IReportService gross)
    : CompositeReportService(rootConfiguration, [payment, gross]);
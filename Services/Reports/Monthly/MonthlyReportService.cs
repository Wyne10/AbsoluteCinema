using AbsoluteCinema.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Monthly;

public sealed class MonthlyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    [FromKeyedServices("monthlyPayment")] MonthlyPaymentReportService payment,
    [FromKeyedServices("monthlyGross")] MonthlyGrossReportService gross)
    : CompositeReportService(rootConfiguration, [payment, gross]);
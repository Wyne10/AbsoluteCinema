using AbsoluteCinema.Configuration;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Monthly;

public sealed class MonthlyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    MonthlyPaymentReportService payment,
    MonthlyGrossReportService gross)
    : CompositeReportService(rootConfiguration, [payment, gross]);
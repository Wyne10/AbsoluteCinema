using AbsoluteCinema.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Weekly;

public sealed class WeeklyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    [FromKeyedServices("weeklyCard")] IReportService card,
    [FromKeyedServices("weeklyCashier")] IReportService cashier,
    [FromKeyedServices("weeklyRentals")] IReportService rentals)
    : CompositeReportService(rootConfiguration, [card, cashier, rentals]);
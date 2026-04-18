using AbsoluteCinema.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Weekly;

public sealed class WeeklyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    [FromKeyedServices("weeklyCard")] WeeklyCardReportService card,
    [FromKeyedServices("weeklyCashier")] WeeklyCashierReportService cashier,
    [FromKeyedServices("weeklyRentals")] WeeklyRentalsReportService rentals)
    : CompositeReportService(rootConfiguration, [card, cashier, rentals]);
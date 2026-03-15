using AbsoluteCinema.Configuration;
using Microsoft.Extensions.Options;

namespace AbsoluteCinema.Services.Reports.Weekly;

public sealed class WeeklyReportService(
    IOptionsMonitor<DocumentRootConfiguration> rootConfiguration,
    WeeklyCardReportService card,
    WeeklyCashierReportService cashier,
    WeeklyRentalsReportService rentals)
    : CompositeReportService(rootConfiguration, [card, cashier, rentals]);
namespace AbsoluteCinema.Services.Reports.Weekly;

public sealed class WeeklyReportService(
    WeeklyCardReportService card,
    WeeklyCashierReportService cashier,
    WeeklyRentalsReportService rentals)
    : CompositeReportService([card, cashier, rentals]);
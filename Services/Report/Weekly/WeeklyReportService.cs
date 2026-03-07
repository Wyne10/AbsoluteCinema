namespace AbsoluteCinema.Services.Report.Weekly;

public sealed class WeeklyReportService(
    WeeklyCardReportService card,
    WeeklyCashierReportService cashier,
    WeeklyRentalsReportService rentals)
    : CompositeReportService([card, cashier, rentals]);
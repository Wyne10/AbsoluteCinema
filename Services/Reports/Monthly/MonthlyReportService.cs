namespace AbsoluteCinema.Services.Reports.Monthly;

public sealed class MonthlyReportService(
    MonthlyPaymentReportService payment,
    MonthlyGrossReportService gross)
    : CompositeReportService([payment, gross]);
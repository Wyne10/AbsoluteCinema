using System;
using System.Linq;
using AbsoluteCinema.ViewModels;
using Avalonia.Controls;

namespace AbsoluteCinema.Views;

public partial class ReportsView : UserControl
{
    private bool _suppressSync;

    public ReportsView()
    {
        InitializeComponent();

        PeriodCalendar.SelectedDatesChanged += (_, _) =>
        {
            if (_suppressSync) return;

            var dates = PeriodCalendar.SelectedDates.OrderBy(d => d).ToList();
            StartDateText.Text = dates.Count > 0 ? $"Начало: {dates.First():yyyy-MM-dd}" : "Начало: —";
            EndDateText.Text = dates.Count > 0 ? $"Конец: {dates.Last():yyyy-MM-dd}" : "Конец: —";

            if (DataContext is ReportsViewModel vm && dates.Count >= 2)
            {
                vm.PeriodStart = dates.First();
                vm.PeriodEnd = dates.Last();
            }
        };
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        RestoreCalendarSelection();
    }

    private void RestoreCalendarSelection()
    {
        if (DataContext is not ReportsViewModel { PeriodStart: { } start, PeriodEnd: { } end }) return;

        _suppressSync = true;
        PeriodCalendar.SelectedDates.Clear();
        for (var d = start; d <= end; d = d.AddDays(1))
            PeriodCalendar.SelectedDates.Add(d);
        _suppressSync = false;

        StartDateText.Text = $"Начало: {start:yyyy-MM-dd}";
        EndDateText.Text = $"Конец: {end:yyyy-MM-dd}";
    }
}

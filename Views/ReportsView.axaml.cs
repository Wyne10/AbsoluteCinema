using System.Linq;
using AbsoluteCinema.ViewModels;
using Avalonia.Controls;

namespace AbsoluteCinema.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        PeriodCalendar.SelectedDatesChanged += (_, _) => UpdatePeriodText();
        PeriodCalendar.SelectedDatesChanged += (_, _) =>
        {
            if (DataContext is ReportsViewModel vm)
                vm.OnSelectedDatesChanged(PeriodCalendar.SelectedDates);
        };
    }

    private void UpdatePeriodText()
    {
        var dates = PeriodCalendar.SelectedDates.OrderBy(d => d).ToList();
        StartDateText.Text = dates.Count > 0 ? $"Начало: {dates.First():yyyy-MM-dd}" : "Начало: —";
        EndDateText.Text = dates.Count > 0 ? $"Конец: {dates.Last():yyyy-MM-dd}" : "Конец: —";
    }
}
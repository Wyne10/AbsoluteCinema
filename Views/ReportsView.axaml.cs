using System.ComponentModel;
using System.Linq;
using AbsoluteCinema.ViewModels;
using AbsoluteCinema.ViewModels.Preview;
using Avalonia.Controls;

namespace AbsoluteCinema.Views;

public partial class ReportsView : UserControl
{
    private readonly ExcelPreviewRenderer? _excelRenderer;

    public ReportsView()
    {
        InitializeComponent();

        _excelRenderer = new ExcelPreviewRenderer(ExcelDataGrid);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ReportsViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };

        PeriodCalendar.SelectedDatesChanged += (_, _) =>
        {
            UpdatePeriodText();
            if (DataContext is ReportsViewModel vm)
                vm.OnSelectedDatesChanged(PeriodCalendar.SelectedDates);
        };

        FilesListBox.DoubleTapped += (_, _) =>
        {
            if (DataContext is ReportsViewModel vm)
                vm.OpenReport(this);
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ReportsViewModel vm) return;
        if (e.PropertyName is not nameof(ReportsViewModel.PreviewFilePath)) return;
        
        if (vm is { CurrentPreview: PreviewType.Excel, PreviewFilePath: not null })
            _excelRenderer?.RenderAsync(vm.PreviewFilePath);
        else
            _excelRenderer?.Clear();
    }

    private void UpdatePeriodText()
    {
        var dates = PeriodCalendar.SelectedDates.OrderBy(d => d).ToList();
        StartDateText.Text = dates.Count > 0 ? $"Начало: {dates.First():yyyy-MM-dd}" : "Начало: —";
        EndDateText.Text = dates.Count > 0 ? $"Конец: {dates.Last():yyyy-MM-dd}" : "Конец: —";
    }
}
using System.ComponentModel;
using AbsoluteCinema.ViewModels;
using AbsoluteCinema.ViewModels.Preview;
using Avalonia.Controls;

namespace AbsoluteCinema.Views;

public partial class PreviewPanelView : UserControl
{
    private readonly ExcelPreviewRenderer _excelRenderer;

    public PreviewPanelView()
    {
        InitializeComponent();

        _excelRenderer = new ExcelPreviewRenderer(ExcelDataGrid);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is FilePreviewViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FilePreviewViewModel vm) return;
        if (e.PropertyName is not nameof(FilePreviewViewModel.PreviewFilePath)) return;

        if (vm is { CurrentPreview: PreviewType.Excel, PreviewFilePath: not null })
            _excelRenderer.RenderAsync(vm.PreviewFilePath);
        else
            _excelRenderer.Clear();
    }
}

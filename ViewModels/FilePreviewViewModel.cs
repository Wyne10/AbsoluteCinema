using System;
using System.Collections.ObjectModel;
using AbsoluteCinema.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AbsoluteCinema.ViewModels;

public abstract partial class FilePreviewViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime? _periodStart;

    [ObservableProperty]
    private DateTime? _periodEnd;

    protected bool HasValidPeriod => PeriodStart is not null && PeriodEnd is not null;

    partial void OnPeriodStartChanged(DateTime? value) => OnPeriodChanged();
    partial void OnPeriodEndChanged(DateTime? value) => OnPeriodChanged();

    protected virtual void OnPeriodChanged() { }

    public ObservableCollection<DocumentFile> DocumentFiles { get; } = [];

    [ObservableProperty]
    private DocumentFile? _selectedFile;

    [ObservableProperty]
    private PreviewType _currentPreview = PreviewType.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PdfPreviewPath))]
    private string? _previewFilePath;

    public string? PdfPreviewPath => CurrentPreview == PreviewType.Pdf ? PreviewFilePath : null;

    partial void OnSelectedFileChanged(DocumentFile? value)
    {
        if (value is null)
        {
            CurrentPreview = PreviewType.None;
            return;
        }
        CurrentPreview = value.Extension switch
        {
            ".pdf" => PreviewType.Pdf,
            ".xlsx" or ".xls" => PreviewType.Excel,
            _ => PreviewType.Unsupported
        };
        PreviewFilePath = value.Path;
    }

    public void OpenReport(Visual visual)
    {
        if (SelectedFile == null) return;
        TopLevel.GetTopLevel(visual)?.Launcher.LaunchUriAsync(new Uri(SelectedFile.Path));
    }

    [RelayCommand]
    private void OpenFilesFolder()
    {
        var path = GetFilesFolderPath();
        if (path is null) return;

        if (System.IO.Directory.Exists(path) == false)
            System.IO.Directory.CreateDirectory(path);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            desktop.MainWindow.Launcher.LaunchUriAsync(new Uri(path));
    }

    protected abstract string? GetFilesFolderPath();
}

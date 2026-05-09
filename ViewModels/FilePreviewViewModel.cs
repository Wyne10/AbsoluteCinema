using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using AbsoluteCinema.Models;
using Avalonia;
using Avalonia.Platform.Storage;
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

    public async Task OpenFile(Visual visual)
    {
        if (SelectedFile == null) return;
        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel is null) return;
        await topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(SelectedFile.Path));
    }

    [RelayCommand]
    private async Task OpenFilesFolder()
    {
        var path = GetFilesFolderPath();
        if (path is null) return;

        if (Directory.Exists(path) == false)
            Directory.CreateDirectory(path);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            await desktop.MainWindow.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(path));
    }

    protected abstract string? GetFilesFolderPath();
}

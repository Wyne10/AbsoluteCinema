using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Report;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.ViewModels;

public partial class ReportsViewModel(IServiceProvider serviceProvider, ILogger<ReportsViewModel> logger)
    : ViewModelBase
{
    private SelectedDatesCollection? _selectedDates;

    public string[] ReportTypes { get; } = ["Еженедельный", "Ежемесячный", "Ежеквартальный"];
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string? _selectedReportType;

    public ObservableCollection<ReportFile> ReportFiles { get; } = [];
    
    [ObservableProperty]
    private ReportFile? _selectedFile;
    
    [ObservableProperty]
    private PreviewType _currentPreview = PreviewType.None;

    [ObservableProperty] 
    private string? _previewFilePath;

    private IReportService? _reportService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isGenerating;

    private List<ReportFile> GetCurrentReports()
    {
        if (_selectedDates?.Count < 2 || _reportService == null) return [];
        var reportsPath = _reportService.GetSessionPath(_selectedDates.First(), _selectedDates.Last());
        if (!Directory.Exists(reportsPath)) return [];
        var filePaths = Directory.EnumerateFiles(reportsPath);
        var items = filePaths.OrderBy(p => p)
            .Select(path => new ReportFile(path)).ToList();
        return items;
    }

    private void RefreshCurrentReports()
    {
        ReportFiles.Clear();
        ReportFiles.AddRange(GetCurrentReports());
    }

    public void OnSelectedDatesChanged(SelectedDatesCollection value)
    {
        if (value.Count < 2) return;
        _selectedDates = value;
        RefreshCurrentReports();
        GenerateReportCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedReportTypeChanged(string? value)
    {
        if (value is null) return;
        var service = value switch
        {
            "Еженедельный" => serviceProvider.GetRequiredKeyedService<IReportService>("weekly"),
            "Ежемесячный" => serviceProvider.GetRequiredKeyedService<IReportService>("monthly"),
            "Ежеквартальный" => serviceProvider.GetRequiredKeyedService<IReportService>("quarterly"),
            _ => _reportService
        };
        _reportService = service;
        _reportService?.OnDownloadProgress += RefreshCurrentReports;
        RefreshCurrentReports();
    }

    partial void OnSelectedFileChanged(ReportFile? value)
    {
        if (value is null)
        {
            CurrentPreview = PreviewType.None;
            return;
        }
        PreviewFilePath = value.Path;
        CurrentPreview = value.Extension switch
        {
            ".pdf" => PreviewType.Pdf,
            ".xlsx" or ".xls" => PreviewType.Excel,
            _ => PreviewType.Unsupported
        };
    }

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReport()
    {
        if (!CanGenerateReport()) return;
        try
        {
            IsGenerating = true;
            using var reportProvider = new ReportProvider();
            await _reportService!.GenerateReportFiles(_selectedDates.First(), _selectedDates.Last(), reportProvider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report generation failed");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    public bool CanGenerateReport()
    {
        return _selectedDates?.Count >= 2 && _reportService is not null && !IsGenerating;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Reports;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReportProvider = AbsoluteCinema.Services.Reports.ReportProvider;

namespace AbsoluteCinema.ViewModels;

public partial class ReportsViewModel(IServiceProvider serviceProvider, ILogger<ReportsViewModel> logger, IHostApplicationLifetime lifetime)
    : FilePreviewViewModel
{
    public string[] ReportTypes { get; } = ["Еженедельный", "Ежемесячный", "Ежеквартальный"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private string? _selectedReportType;

    private IReportService? _reportService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool _isGenerating;

    private List<DocumentFile> GetCurrentReports()
    {
        if (!HasValidPeriod || _reportService == null) return [];
        var reportsPath = _reportService.GetSessionPath(PeriodStart!.Value, PeriodEnd!.Value);
        if (!Directory.Exists(reportsPath)) return [];
        var filePaths = Directory.EnumerateFiles(reportsPath);
        var items = filePaths.OrderBy(p => p)
            .Select(path => new DocumentFile(path)).ToList();
        return items;
    }

    private void RefreshCurrentReports()
    {
        DocumentFiles.Clear();
        DocumentFiles.AddRange(GetCurrentReports());
    }

    protected override void OnPeriodChanged()
    {
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

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReport()
    {
        if (!CanGenerateReport()) return;
        try
        {
            IsGenerating = true;
            using var reportProvider = new ReportProvider(logger);
            await _reportService!.GenerateReportFiles(PeriodStart!.Value, PeriodEnd!.Value, reportProvider, lifetime.ApplicationStopping);
            logger.LogInformation("Report generated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Report generation failed");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"При генерации отчета произошла ошибка: {ex.Message}", ButtonEnum.Ok,
                    Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsGenerating = false;
        }
    }

    public bool CanGenerateReport() =>
        HasValidPeriod && _reportService is not null && !IsGenerating;

    protected override string GetFilesFolderPath()
    {
        string? pathToOpen = null;

        if (HasValidPeriod && _reportService is not null)
        {
            var sessionPath = _reportService.GetSessionPath(PeriodStart!.Value, PeriodEnd!.Value);
            pathToOpen = Directory.Exists(sessionPath) ? sessionPath : Path.GetDirectoryName(sessionPath);
        }

        pathToOpen ??= Path.GetTempPath();
        return pathToOpen;
    }
}

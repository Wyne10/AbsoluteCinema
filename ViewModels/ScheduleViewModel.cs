using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Movies;
using AbsoluteCinema.Services.Schedule;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace AbsoluteCinema.ViewModels;

public partial class ScheduleViewModel(IScheduleService scheduleService, ILogger<ScheduleViewModel> logger, IHostApplicationLifetime lifetime) : FilePreviewViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateScheduleCommand))]
    private bool _isGenerating;
    
    private List<DocumentFile> GetCurrentDocuments()
    {
        if (!HasValidPeriod) return [];
        var schedulePath = scheduleService.GetSessionPath(PeriodStart!.Value, PeriodEnd!.Value);
        if (!Directory.Exists(schedulePath)) return [];
        var filePaths = Directory.EnumerateFiles(schedulePath);
        var items = filePaths.OrderBy(p => p)
            .Select(path => new DocumentFile(path)).ToList();
        return items;
    }
    
    private void RefreshCurrentDocuments()
    {
        DocumentFiles.Clear();
        DocumentFiles.AddRange(GetCurrentDocuments());
    }
    
    protected override void OnPeriodChanged()
    {
        RefreshCurrentDocuments();
        GenerateScheduleCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateSchedule))]
    private async Task GenerateSchedule()
    {
        if (!CanGenerateSchedule()) return;
        try
        {
            IsGenerating = true;
            using var movieProvider = new CinemaWebMovieProvider(logger);
            await scheduleService.GenerateScheduleFiles(PeriodStart!.Value, PeriodEnd!.Value, movieProvider, lifetime.ApplicationStopping);
            RefreshCurrentDocuments();
            logger.LogInformation("Schedule generated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schedule generation failed");
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", $"При генерации расписания произошла ошибка: {ex.Message}", ButtonEnum.Ok,
                    Icon.Error);
            await box.ShowAsync();
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private bool CanGenerateSchedule() => 
        HasValidPeriod && !IsGenerating;

    protected override string GetFilesFolderPath()
    {
        string? pathToOpen = null;

        if (HasValidPeriod)
        {
            var sessionPath = scheduleService.GetSessionPath(PeriodStart!.Value, PeriodEnd!.Value);
            pathToOpen = Directory.Exists(sessionPath) ? sessionPath : Path.GetDirectoryName(sessionPath);
        }

        pathToOpen ??= Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        return pathToOpen;
    }
}

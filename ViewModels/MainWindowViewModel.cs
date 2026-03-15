using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace AbsoluteCinema.ViewModels;

public partial class MainWindowViewModel(
    ReportsViewModel reportsViewModel,
    ScheduleViewModel scheduleViewModel,
    IServiceProvider serviceProvider) : ViewModelBase
{
    private readonly ViewModelBase[] _pages = [reportsViewModel, scheduleViewModel];

    [ObservableProperty]
    private ViewModelBase _currentPage = reportsViewModel;

    [ObservableProperty]
    private int _selectedNavIndex;

    partial void OnSelectedNavIndexChanged(int value)
    {
        if (value == 5)
        {
            var settings = serviceProvider.GetRequiredService<SettingsViewModel>();
            settings.Load();
            CurrentPage = settings;
        }
        else if (value >= 0 && value < _pages.Length)
        {
            CurrentPage = _pages[value];
        }
    }
}

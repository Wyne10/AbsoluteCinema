using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace AbsoluteCinema.ViewModels;

public partial class MainWindowViewModel(
    ReportsViewModel reportsViewModel,
    ScheduleViewModel scheduleViewModel,
    TrailersViewModel trailersViewModel,
    MailingViewModel mailingViewModel,
    VkAnnouncementsViewModel vkAnnouncementsViewModel,
    IServiceProvider serviceProvider) : ViewModelBase
{
    private readonly ViewModelBase[] _pages = [reportsViewModel, scheduleViewModel, trailersViewModel, mailingViewModel, vkAnnouncementsViewModel];

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

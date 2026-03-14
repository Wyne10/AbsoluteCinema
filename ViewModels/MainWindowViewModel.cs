using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.ViewModels;

public partial class MainWindowViewModel(
    ReportsViewModel reportsViewModel,
    ScheduleViewModel scheduleViewModel) : ViewModelBase
{
    private readonly ViewModelBase[] _pages = [reportsViewModel, scheduleViewModel];

    [ObservableProperty]
    private ViewModelBase _currentPage = reportsViewModel;

    [ObservableProperty]
    private int _selectedNavIndex;

    partial void OnSelectedNavIndexChanged(int value)
    {
        if (value >= 0 && value < _pages.Length)
            CurrentPage = _pages[value];
    }
}

namespace AbsoluteCinema.ViewModels;

public partial class MainWindowViewModel(ReportsViewModel reportsViewModel) : ViewModelBase
{
    public ReportsViewModel ReportsViewModel { get; } = reportsViewModel;
}

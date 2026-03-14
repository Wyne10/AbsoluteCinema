using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace AbsoluteCinema.ViewModels;

public partial class ScheduleViewModel : FilePreviewViewModel
{
    protected override void OnPeriodChanged()
    {
        GenerateScheduleCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGenerateSchedule))]
    private Task GenerateSchedule()
    {
        // TODO: implement schedule generation
        return Task.CompletedTask;
    }

    private bool CanGenerateSchedule() => HasValidPeriod;

    protected override string GetFilesFolderPath()
    {
        return Path.Combine(Path.GetTempPath(), "AbsoluteCinema", "Schedule");
    }
}

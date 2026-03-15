using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.Models;

public partial class SettingField(string path, string displayName, string groupName) : ObservableObject
{
    public string Path { get; } = path;
    public string DisplayName { get; } = displayName;
    public string GroupName { get; } = groupName;

    [ObservableProperty]
    private string _value = string.Empty;
}

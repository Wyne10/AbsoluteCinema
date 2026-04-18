using AbsoluteCinema.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.ViewModels;

public partial class SelectableContact(EmailContact contact) : ObservableObject
{
    public EmailContact Contact { get; } = contact;

    [ObservableProperty]
    private bool _isSelected;
}

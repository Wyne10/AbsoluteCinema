using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.Models;

public partial class EmailContact : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;
}

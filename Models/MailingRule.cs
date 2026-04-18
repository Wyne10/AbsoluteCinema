using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AbsoluteCinema.Models;

public partial class MailingRule : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _serviceKey = string.Empty;

    [ObservableProperty]
    private MailingFrequency _frequency;

    [ObservableProperty]
    private string _subject = string.Empty;

    public List<Guid> ContactIds { get; set; } = [];
}

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AbsoluteCinema.Dtos;
using AbsoluteCinema.Models;
using AbsoluteCinema.Services.Mailing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.ViewModels;

public partial class MailingViewModel : ViewModelBase
{
    private readonly MailingStorage _storage;
    private readonly MailingSender _sender;
    private readonly ILogger<MailingViewModel> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public MailingViewModel(
        MailingStorage storage,
        MailingSender sender,
        ILogger<MailingViewModel> logger,
        IHostApplicationLifetime lifetime)
    {
        _storage = storage;
        _sender = sender;
        _logger = logger;
        _lifetime = lifetime;

        Load();
    }

    // Contacts
    public ObservableCollection<EmailContact> Contacts { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveContactCommand))]
    private EmailContact? _selectedContact;

    [ObservableProperty]
    private string _newContactName = string.Empty;

    [ObservableProperty]
    private string _newContactEmail = string.Empty;

    // Rules
    public ObservableCollection<MailingRule> Rules { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveRuleCommand))]
    [NotifyCanExecuteChangedFor(nameof(SendRuleCommand))]
    private MailingRule? _selectedRule;

    [ObservableProperty]
    private MailableService? _selectedService;

    [ObservableProperty]
    private MailingFrequency _selectedFrequency;

    // Rule editor
    public ObservableCollection<SelectableContact> RuleContacts { get; } = [];

    // Available options
    public static MailableService[] AvailableServices { get; } =
    [
        new("weekly", "Еженедельный (полный)", MailableServiceCategory.Report),
        new("weeklyCard", "Еженедельный (пушкинская)", MailableServiceCategory.Report),
        new("weeklyCashier", "Еженедельный (кассир)", MailableServiceCategory.Report),
        new("weeklyRentals", "Еженедельный (сводный)", MailableServiceCategory.Report),
        new("monthly", "Ежемесячный (полный)", MailableServiceCategory.Report),
        new("monthlyGross", "Ежемесячный (сборы)", MailableServiceCategory.Report),
        new("monthlyPayment", "Ежемесячный (оплаты)", MailableServiceCategory.Report),
        new("quarterly", "Ежеквартальный", MailableServiceCategory.Report),
        new("schedule", "Расписание", MailableServiceCategory.Schedule),
    ];

    public static MailingFrequency[] AvailableFrequencies { get; } = Enum.GetValues<MailingFrequency>();

    // State
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendRuleCommand))]
    private bool _isSending;

    [ObservableProperty]
    private string _statusText = string.Empty;

    // Contacts CRUD

    [RelayCommand]
    private void AddContact()
    {
        if (string.IsNullOrWhiteSpace(NewContactName) || string.IsNullOrWhiteSpace(NewContactEmail))
            return;

        var contact = new EmailContact { Name = NewContactName, Email = NewContactEmail };
        Contacts.Add(contact);
        NewContactName = string.Empty;
        NewContactEmail = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveContact))]
    private void RemoveContact()
    {
        if (SelectedContact is null) return;

        // Remove from any rules
        foreach (var rule in Rules)
            rule.ContactIds.Remove(SelectedContact.Id);

        Contacts.Remove(SelectedContact);
        RefreshRuleContacts();
    }

    private bool CanRemoveContact() => SelectedContact is not null;

    // Rules CRUD

    [RelayCommand]
    private void AddRule()
    {
        var rule = new MailingRule
        {
            ServiceKey = AvailableServices[0].Key,
            Frequency = MailingFrequency.Weekly
        };
        Rules.Add(rule);
        SelectedRule = rule;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveRule))]
    private void RemoveRule()
    {
        if (SelectedRule is null) return;
        Rules.Remove(SelectedRule);
        SelectedRule = null;
    }

    private bool CanRemoveRule() => SelectedRule is not null;

    partial void OnSelectedRuleChanged(MailingRule? value)
    {
        if (value is not null)
        {
            SelectedService = Array.Find(AvailableServices, s => s.Key == value.ServiceKey);
            SelectedFrequency = value.Frequency;
        }
        RefreshRuleContacts();
    }

    partial void OnSelectedServiceChanged(MailableService? value)
    {
        if (SelectedRule is not null && value is not null)
            SelectedRule.ServiceKey = value.Key;
    }

    partial void OnSelectedFrequencyChanged(MailingFrequency value)
    {
        if (SelectedRule is not null)
            SelectedRule.Frequency = value;
    }

    private void RefreshRuleContacts()
    {
        RuleContacts.Clear();
        if (SelectedRule is null) return;

        foreach (var contact in Contacts)
        {
            var selectable = new SelectableContact(contact)
            {
                IsSelected = SelectedRule.ContactIds.Contains(contact.Id)
            };
            selectable.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(SelectableContact.IsSelected) || SelectedRule is null) return;
                if (selectable.IsSelected)
                {
                    if (!SelectedRule.ContactIds.Contains(contact.Id))
                        SelectedRule.ContactIds.Add(contact.Id);
                }
                else
                {
                    SelectedRule.ContactIds.Remove(contact.Id);
                }
            };
            RuleContacts.Add(selectable);
        }
    }

    // Send

    [RelayCommand(CanExecute = nameof(CanSendRule))]
    private async Task SendRule()
    {
        if (SelectedRule is null) return;

        try
        {
            IsSending = true;
            StatusText = "Генерация файлов и отправка...";
            await _sender.SendRuleAsync(SelectedRule, Contacts, _lifetime.ApplicationStopping);
            StatusText = "Отправка завершена";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rule {RuleId}", SelectedRule.Id);
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSendRule() => SelectedRule is not null && !IsSending;

    // Persistence

    [RelayCommand]
    private void Save()
    {
        var data = new MailingData
        {
            Contacts = Contacts.Select(c => new EmailContactDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email
            }).ToList(),
            Rules = Rules.Select(r => new MailingRuleDto
            {
                Id = r.Id,
                ServiceKey = r.ServiceKey,
                Frequency = r.Frequency,
                Subject = r.Subject,
                ContactIds = [..r.ContactIds]
            }).ToList()
        };
        _storage.Save(data);
        StatusText = "Сохранено";
    }

    private void Load()
    {
        var data = _storage.Load();

        Contacts.Clear();
        foreach (var dto in data.Contacts)
        {
            Contacts.Add(new EmailContact
            {
                Id = dto.Id,
                Name = dto.Name,
                Email = dto.Email
            });
        }

        Rules.Clear();
        foreach (var dto in data.Rules)
        {
            Rules.Add(new MailingRule
            {
                Id = dto.Id,
                ServiceKey = dto.ServiceKey,
                Frequency = dto.Frequency,
                Subject = dto.Subject,
                ContactIds = [..dto.ContactIds]
            });
        }
    }
}

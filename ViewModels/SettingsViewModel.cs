using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AbsoluteCinema.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AbsoluteCinema.ViewModels;

public partial class SettingsViewModel(ILogger<SettingsViewModel> logger) : ViewModelBase
{
    private static readonly HashSet<string> ExcludedSections = ["Serilog"];

    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        ["Movie:ApiToken"] = "API-токен Poiskkino",
        ["Report:Root:RootPath"] = "Папка отчётов",
        ["Report:Monthly:TemplatePath"] = "Шаблон ежемесячного отчёта",
        ["Report:Quarterly:TemplatePath"] = "Шаблон ежеквартального отчёта",
        ["Schedule:Root:RootPath"] = "Папка расписаний",
        ["Schedule:Repertoire:TemplatePath"] = "Шаблон репертуара",
    };

    private static readonly Dictionary<string, string> GroupNames = new()
    {
        ["Movie"] = "Фильмы",
        ["Report"] = "Отчёты",
        ["Schedule"] = "Расписание",
    };

    public ObservableCollection<SettingField> Settings { get; } = [];

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    private string _appSettingsPath = string.Empty;

    public void Load()
    {
        _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Settings.Clear();

        if (!File.Exists(_appSettingsPath))
        {
            logger.LogWarning("appsettings.json not found at {Path}", _appSettingsPath);
            return;
        }

        var json = File.ReadAllText(_appSettingsPath);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null) return;

        FlattenJson(root, string.Empty);
        HasUnsavedChanges = false;
    }

    private void FlattenJson(JsonObject obj, string prefix)
    {
        foreach (var (key, value) in obj)
        {
            var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
            var topSection = path.Split(':')[0];

            if (ExcludedSections.Contains(topSection))
                continue;

            if (value is JsonObject nested)
            {
                FlattenJson(nested, path);
            }
            else
            {
                var displayName = DisplayNames.GetValueOrDefault(path, key);
                var groupName = GroupNames.GetValueOrDefault(topSection, topSection);
                var field = new SettingField(path, displayName, groupName)
                {
                    Value = value?.ToString() ?? string.Empty
                };
                field.PropertyChanged += (_, _) => HasUnsavedChanges = true;
                Settings.Add(field);
            }
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var json = File.ReadAllText(_appSettingsPath);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root is null) return;

            foreach (var field in Settings)
            {
                SetJsonValue(root, field.Path, field.Value);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_appSettingsPath, root.ToJsonString(options));
            HasUnsavedChanges = false;
            logger.LogInformation("Settings saved to {Path}", _appSettingsPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save settings");
        }
    }

    [RelayCommand]
    private void Reset()
    {
        Load();
    }

    private static void SetJsonValue(JsonObject root, string path, string value)
    {
        var segments = path.Split(':');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is not JsonObject next)
            {
                next = new JsonObject();
                current[segments[i]] = next;
            }
            current = next;
        }

        current[segments[^1]] = value;
    }

    public IEnumerable<IGrouping<string, SettingField>> GroupedSettings =>
        Settings.GroupBy(s => s.GroupName);
}
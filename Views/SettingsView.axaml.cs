using System;
using System.Linq;
using AbsoluteCinema.ViewModels;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace AbsoluteCinema.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is SettingsViewModel vm)
            vm.Settings.CollectionChanged += (_, _) => BuildSettingsUi();

        BuildSettingsUi();
    }

    private void BuildSettingsUi()
    {
        SettingsPanel.Children.Clear();

        if (DataContext is not SettingsViewModel vm) return;

        foreach (var group in vm.GroupedSettings)
        {
            var border = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
            };
            border.Bind(Border.BackgroundProperty, border.GetResourceObservable("SystemControlBackgroundChromeMediumLowBrush"));

            var stack = new StackPanel { Spacing = 16 };

            stack.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontSize = 16,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            });

            foreach (var field in group)
            {
                var fieldStack = new StackPanel { Spacing = 6 };

                fieldStack.Children.Add(new TextBlock
                {
                    Text = field.DisplayName,
                    Opacity = 0.7,
                    FontSize = 12,
                });

                var textBox = new TextBox
                {
                    MinWidth = 400,
                    MaxWidth = 500,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                textBox.Bind(TextBox.TextProperty, new Binding(nameof(field.Value)) { Source = field, Mode = BindingMode.TwoWay });

                fieldStack.Children.Add(textBox);
                stack.Children.Add(fieldStack);
            }

            border.Child = stack;
            SettingsPanel.Children.Add(border);
        }
    }
}

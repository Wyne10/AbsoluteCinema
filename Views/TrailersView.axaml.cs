using AbsoluteCinema.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace AbsoluteCinema.Views;

public partial class TrailersView : UserControl
{
    public TrailersView()
    {
        InitializeComponent();
        RenderedTrailersListBox.DoubleTapped += OnRenderedTrailerDoubleTapped;
    }

    private async void OnRenderedTrailerDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is TrailersViewModel vm)
            await vm.OpenSelectedTrailer(this);
    }
}
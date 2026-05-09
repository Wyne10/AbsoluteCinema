using AbsoluteCinema.ViewModels;
using Avalonia.Controls;

namespace AbsoluteCinema.Views;

public partial class FilesListView : UserControl
{
    public FilesListView()
    {
        InitializeComponent();

        FilesListBox.DoubleTapped += async (_, _) =>
        {
            if (DataContext is FilePreviewViewModel vm)
                await vm.OpenFile(this);
        };
    }
}

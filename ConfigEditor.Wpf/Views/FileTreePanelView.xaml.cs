using ConfigEditor.Wpf.Models;
using ConfigEditor.Wpf.ViewModels;

namespace ConfigEditor.Wpf.Views;

public partial class FileTreePanelView : System.Windows.Controls.UserControl
{
    public FileTreePanelView()
    {
        InitializeComponent();
    }

    private void TreeView_OnSelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (e.NewValue is not FileTreeNode selectedNode)
        {
            return;
        }

        viewModel.HandleTreeSelection(selectedNode);
    }
}

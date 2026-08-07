using ConfigEditor.Wpf.Models;
using ConfigEditor.Wpf.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ConfigEditor.Wpf.Views;

public partial class SingleFileEditorView : System.Windows.Controls.UserControl
{
    private MainViewModel? _boundViewModel;

    public SingleFileEditorView()
    {
        InitializeComponent();
        DataContextChanged += SingleFileEditorView_DataContextChanged;
        Unloaded += SingleFileEditorView_Unloaded;
    }

    private void XmlTree_OnSelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (e.NewValue is XmlElementNode elementNode)
        {
            viewModel.HandleXmlNodeSelection(elementNode);
        }
        else if (e.NewValue is XmlCommentNode commentNode)
        {
            viewModel.HandleXmlCommentSelection(commentNode);
        }
        else
        {
            viewModel.HandleXmlNodeSelection(null);
        }
    }

    private void SingleFileEditorView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_boundViewModel is not null)
        {
            _boundViewModel.PropertyChanged -= BoundViewModel_PropertyChanged;
        }

        _boundViewModel = e.NewValue as MainViewModel;
        if (_boundViewModel is not null)
        {
            _boundViewModel.PropertyChanged += BoundViewModel_PropertyChanged;
        }
    }

    private void BoundViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedSearchResult)
            or nameof(MainViewModel.SelectedXmlNode))
        {
            if (_boundViewModel?.IsPlainTextMode == true)
            {
                ScrollPlainTextToSearchResult();
            }
            else
            {
                ScrollSelectedNodeIntoView();
            }
        }
    }

    private void SingleFileEditorView_Unloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_boundViewModel is null)
        {
            return;
        }

        _boundViewModel.PropertyChanged -= BoundViewModel_PropertyChanged;
        _boundViewModel = null;
    }

    private void ScrollSelectedNodeIntoView()
    {
        if (DataContext is not MainViewModel viewModel || viewModel.SelectedXmlNode is null)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            var treeViewItem = FindTreeViewItem(XmlTree, viewModel.SelectedXmlNode);
            treeViewItem?.BringIntoView();
        }, DispatcherPriority.Background);
    }

    private void ScrollPlainTextToSearchResult()
    {
        if (_boundViewModel?.SelectedSearchResult is not { TextOffset: >= 0 } result)
        {
            return;
        }

        var queryLen = (_boundViewModel.XmlSearchText ?? string.Empty).Length;

        Dispatcher.BeginInvoke(() =>
        {
            PlainTextBox.Focus();
            PlainTextBox.Select(result.TextOffset, queryLen);
            var line = PlainTextBox.GetLineIndexFromCharacterIndex(result.TextOffset);
            if (line >= 0) PlainTextBox.ScrollToLine(line);
        }, DispatcherPriority.Background);
    }

    private static TreeViewItem? FindTreeViewItem(ItemsControl parent, object targetItem)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(targetItem) is TreeViewItem direct)
        {
            return direct;
        }

        foreach (var item in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem child)
            {
                continue;
            }

            var result = FindTreeViewItem(child, targetItem);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}

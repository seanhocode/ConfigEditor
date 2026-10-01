using System.Collections.ObjectModel;
using SeanTool.CSharp.WPFTool;

namespace ConfigEditor.Wpf.Models;

/// <summary>
/// 檔案樹狀節點 UI 模型
/// </summary>
public class FileTreeNode : ViewModelBase
{
    private bool _isChecked;
    private bool _isExpanded;

    /// <summary>
    /// 節點顯示名稱
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 檔案完整路徑（資料夾節點可為空）
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// 是否為檔案節點
    /// </summary>
    public bool IsFile { get; init; }

    /// <summary>
    /// 子節點集合
    /// </summary>
    public ObservableCollection<FileTreeNode> Children { get; } = [];

    /// <summary>
    /// 是否勾選
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否展開
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }
}

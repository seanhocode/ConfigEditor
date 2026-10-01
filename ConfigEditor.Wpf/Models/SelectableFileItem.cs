using SeanTool.CSharp.WPFTool;

namespace ConfigEditor.Wpf.Models;

/// <summary>
/// 可勾選檔案項目的 UI 模型
/// </summary>
public class SelectableFileItem : ViewModelBase
{
    private bool _isSelected;

    /// <summary>
    /// 檔案路徑
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// 是否已勾選
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

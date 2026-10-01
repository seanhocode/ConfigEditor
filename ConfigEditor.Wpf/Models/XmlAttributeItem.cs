using SeanTool.CSharp.WPFTool;

namespace ConfigEditor.Wpf.Models;

/// <summary>
/// XML 屬性項目 UI 模型
/// </summary>
public class XmlAttributeItem : ViewModelBase
{
    private string _name = string.Empty;
    private string _value = string.Empty;

    /// <summary>
    /// 屬性名稱
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 屬性值
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }
}

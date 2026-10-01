using System.ComponentModel;
using SeanTool.CSharp.WPFTool;

namespace ConfigEditor.Wpf.Models
{
    /// <summary>
    /// XML 樹狀節點
    /// </summary>
    public class XmlNode : ViewModelBase
    {
        private bool _isExpanded;
        private bool _isSelected;

        /// <summary>
        /// 樹狀顯示標籤
        /// </summary>
        public virtual string DisplayLabel { get; }

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

        /// <summary>
        /// 是否選取
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

        /// <summary>
        /// 父節點
        /// </summary>
        public XmlNode? Parent { get; set; }

        /// <summary>
        /// 節點型別標識（"Element" 或 "Comment"）
        /// </summary>
        public string NodeType { get; protected set; }

        protected void AttributeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(XmlAttributeItem.Name) or nameof(XmlAttributeItem.Value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml.Linq;

namespace ConfigEditor.Wpf.Models;

/// <summary>
/// 可綁定的 XML 節點 UI 模型
/// </summary>
public class XmlElementNode : XmlNode
{
    private string _elementName = string.Empty;
    private string _textValue = string.Empty;

    private XmlElementNode(XElement element, XmlNode? parent) : this()
    {
        Element = element;
        Parent = parent;
        _elementName = element.Name.LocalName;
        _textValue = GetDirectText(element);

        Attributes.CollectionChanged += AttributesOnCollectionChanged;

        foreach (var attribute in element.Attributes())
        {
            Attributes.Add(new XmlAttributeItem
            {
                Name = attribute.Name.LocalName,
                Value = attribute.Value,
            });
        }

        // 同時加入子元素和註解節點（保持文件順序）
        foreach (var node in element.Nodes())
        {
            if (node is XElement childElement)
            {
                Children.Add(new XmlElementNode(childElement, this));
            }
            else if (node is XComment comment)
            {
                Children.Add(new XmlCommentNode(comment, this));
            }
        }
    }

    public XmlElementNode()
    {
        NodeType = "Element";
    }

    /// <summary>
    /// 對應的原始 XElement
    /// </summary>
    public XElement Element { get; }

    /// <summary>
    /// 子節點集合（包含元素和註解）
    /// </summary>
    public ObservableCollection<XmlNode> Children { get; } = new ObservableCollection<XmlNode>();


    /// <summary>
    /// 屬性集合
    /// </summary>
    public ObservableCollection<XmlAttributeItem> Attributes { get; } = new ObservableCollection<XmlAttributeItem>();

    /// <summary>
    /// 是否為根節點
    /// </summary>
    public bool IsRoot => Parent is null;

    /// <summary>
    /// 節點名稱
    /// </summary>
    public string ElementName
    {
        get => _elementName;
        set
        {
            if (_elementName == value)
            {
                return;
            }

            _elementName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }

    /// <summary>
    /// 節點直接文字內容
    /// </summary>
    public string TextValue
    {
        get => _textValue;
        set
        {
            if (_textValue == value)
            {
                return;
            }

            _textValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }

    /// <summary>
    /// 樹狀顯示標籤
    /// </summary>
    public override string DisplayLabel
    {
        get
        {
            var attributeText = string.Join(
                " ",
                Attributes
                    .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name))
                    .Select(attribute => $"{attribute.Name}=\"{attribute.Value}\""));

            var head = string.IsNullOrWhiteSpace(attributeText)
                ? $"<{ElementName}>"
                : $"<{ElementName} {attributeText}>";

            return string.IsNullOrWhiteSpace(TextValue)
                ? head
                : $"{head} {TextValue}";
        }
    }

    /// <summary>
    /// 由 XElement 建立對應節點樹
    /// </summary>
    /// <param name="element">來源元素</param>
    /// <returns>根節點模型</returns>
    public static XmlElementNode FromElement(XElement element)
    {
        return new XmlElementNode(element, null);
    }

    /// <summary>
    /// 新增子節點並回傳新增節點
    /// </summary>
    /// <param name="childName">子節點名稱</param>
    /// <param name="childText">子節點文字內容</param>
    /// <returns>新增後節點</returns>
    public XmlElementNode AddChild(string childName, string childText)
    {
        var validName = string.IsNullOrWhiteSpace(childName) ? "NewElement" : childName.Trim();
        var newElement = new XElement(XName.Get(validName, Element.Name.NamespaceName));
        if (!string.IsNullOrWhiteSpace(childText))
        {
            newElement.Add(new XText(childText));
        }

        Element.Add(newElement);

        var node = new XmlElementNode(newElement, this);
        Children.Add(node);
        return node;
    }

    public XmlElementNode AddChildElement(XElement xElement)
    {
        Element.Add(xElement);
        var node = new XmlElementNode(xElement, this);
        Children.Add(node);
        return node;
    }

    /// <summary>
    /// 以前序方式巡覽此節點與所有子孫元素節點（不含註解）
    /// </summary>
    /// <returns>巡覽結果序列</returns>
    public IEnumerable<XmlElementNode> Traverse()
    {
        yield return this;
        foreach (var child in Children.OfType<XmlElementNode>())
        {
            foreach (var nested in child.Traverse())
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// 遞迴設定展開狀態（含所有子節點）
    /// </summary>
    /// <param name="expanded">是否展開</param>
    public void SetExpandedRecursively(bool expanded)
    {
        IsExpanded = expanded;
        foreach (var child in Children)
        {
            child.IsExpanded = expanded;
            if (child is XmlElementNode elementChild)
            {
                elementChild.SetExpandedRecursively(expanded);
            }
        }
    }

    /// <summary>
    /// 展開所有祖先節點
    /// </summary>
    public void ExpandAncestors()
    {
        var current = Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    /// <summary>
    /// 從父節點移除此節點，並同步移除原始 XElement
    /// </summary>
    public void RemoveFromParent()
    {
        if (Parent is null)
        {
            return;
        }

        if (Parent is XmlElementNode parentElement)
        {
            parentElement.Children.Remove(this);
        }

        Element.Remove();
    }

    /// <summary>
    /// 將目前 UI 模型內容回寫到對應 XElement
    /// </summary>
    public void ApplyToElement()
    {
        var validName = string.IsNullOrWhiteSpace(ElementName) ? Element.Name.LocalName : ElementName.Trim();
        Element.Name = XName.Get(validName, Element.Name.NamespaceName);

        Element.Attributes().Remove();
        foreach (var attribute in Attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                continue;
            }

            Element.SetAttributeValue(XName.Get(attribute.Name.Trim()), attribute.Value ?? string.Empty);
        }

        var directTextNodes = Element.Nodes().OfType<XText>().ToList();
        foreach (var node in directTextNodes)
        {
            node.Remove();
        }

        if (!string.IsNullOrWhiteSpace(TextValue))
        {
            Element.AddFirst(new XText(TextValue));
        }

        // 只對元素子節點遞迴套用（註解已在編輯時同步）
        foreach (var child in Children.OfType<XmlElementNode>())
        {
            child.ApplyToElement();
        }

        OnPropertyChanged(nameof(DisplayLabel));
    }

    private static string GetDirectText(XElement element)
    {
        return string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)).Trim();
    }

    private void AttributesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<XmlAttributeItem>())
            {
                item.PropertyChanged += AttributeOnPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<XmlAttributeItem>())
            {
                item.PropertyChanged -= AttributeOnPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(DisplayLabel));
    }
}

using System.Xml.Linq;

namespace ConfigEditor.Wpf.Models;

/// <summary>
/// 可綁定的 XML 註解節點 UI 模型
/// </summary>
public class XmlCommentNode : XmlNode
{
    private string _commentText = string.Empty;

    /// <summary>
    /// 初始化新的註解節點
    /// </summary>
    /// <param name="comment">來源 XComment 物件</param>
    /// <param name="parent">父節點</param>
    public XmlCommentNode(XComment comment, XmlNode? parent) : this()
    {
        Comment = comment;
        Parent = parent;
        _commentText = comment.Value;
    }

    public XmlCommentNode()
    {
        NodeType = "Comment";
    }

    /// <summary>
    /// 對應的原始 XComment
    /// </summary>
    public XComment Comment { get; }

    /// <summary>
    /// 註解文字內容
    /// </summary>
    public string CommentText
    {
        get => _commentText;
        set
        {
            if (_commentText == value)
            {
                return;
            }

            _commentText = value;
            Comment.Value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }

    /// <summary>
    /// 樹狀顯示標籤
    /// </summary>
    public override string DisplayLabel => $"<!-- {_commentText} -->";

    /// <summary>
    /// 從父節點移除此註解節點，並同步移除原始 XComment
    /// </summary>
    public void RemoveFromParent()
    {
        if (Parent is XmlElementNode parentElement)
        {
            parentElement.Children.Remove(this);
            Comment.Remove();
        }
    }

    /// <summary>
    /// 將註解文字轉換為合法的 XML 元素名稱
    /// 移除或替換非法字符，如空格、特殊符號等
    /// </summary>
    /// <param name="commentText">註解文字</param>
    /// <returns>合法的 XML 元素名稱</returns>
    public static string ConvertCommentToElementName(string commentText)
    {
        if (string.IsNullOrWhiteSpace(commentText))
        {
            return "UncommentedElement";
        }

        // 移除或替換非法 XML 名稱字符
        var cleaned = System.Text.RegularExpressions.Regex.Replace(commentText.Trim(), @"[\s\-.\:@#$%^&*()=+\[\]{};:'"",<>/?\\|`~]", "");

        if (string.IsNullOrEmpty(cleaned))
        {
            return "UncommentedElement";
        }

        // XML 名稱不能以數字、句點或連字號開頭
        while (cleaned.Length > 0 && (char.IsDigit(cleaned[0]) || cleaned[0] == '.' || cleaned[0] == '-'))
        {
            cleaned = cleaned.Substring(1);
        }

        return string.IsNullOrEmpty(cleaned) ? "UncommentedElement" : cleaned;
    }
}

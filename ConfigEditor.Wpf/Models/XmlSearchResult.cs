namespace ConfigEditor.Wpf.Models;

/// <summary>
/// XML 搜尋結果項目
/// </summary>
public class XmlSearchResult
{
    /// <summary>
    /// 命中的節點
    /// </summary>
    public XmlElementNode Node { get; init; } = null!;

    /// <summary>
    /// 命中節點名稱
    /// </summary>
    public string ElementName { get; init; } = string.Empty;

    /// <summary>
    /// 命中節點路徑
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 命中摘要文字
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// 純文字搜尋時的字元偏移量；XML 搜尋時為 -1
    /// </summary>
    public int TextOffset { get; init; } = -1;
}

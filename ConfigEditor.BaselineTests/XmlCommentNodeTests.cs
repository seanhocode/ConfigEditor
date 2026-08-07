using ConfigEditor.Wpf.Models;
using System.Xml.Linq;

namespace ConfigEditor.BaselineTests;

/// <summary>
/// XML 註解節點單元測試
/// </summary>
public class XmlCommentNodeTests
{
    /// <summary>
    /// 測試簡單註解轉換為元素名稱
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_SimpleComment_ReturnsElementName()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("config");
        Assert.Equal("config", result);
    }

    /// <summary>
    /// 測試含空格的註解轉換為元素名稱
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_CommentWithSpaces_RemovesSpaces()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("database config");
        Assert.Equal("databaseconfig", result);
    }

    /// <summary>
    /// 測試含多個空格的長註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_LongCommentWithMultipleSpaces_HandlesProperly()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("  app  settings  value  ");
        Assert.Equal("appsettingsvalue", result);
    }

    /// <summary>
    /// 測試含特殊字符的註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_CommentWithSpecialChars_RemovesSpecialChars()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("config@2024#test");
        Assert.Equal("config2024test", result);
    }

    /// <summary>
    /// 測試以數字開頭的註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_StartsWithDigit_RemovesLeadingDigit()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("2024config");
        Assert.Equal("config", result);
    }

    /// <summary>
    /// 測試以連字號開頭的註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_StartsWithHyphen_RemovesLeadingHyphen()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("-config");
        Assert.Equal("config", result);
    }

    /// <summary>
    /// 測試以句點開頭的註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_StartsWithDot_RemovesLeadingDot()
    {
        var result = XmlCommentNode.ConvertCommentToElementName(".config");
        Assert.Equal("config", result);
    }

    /// <summary>
    /// 測試完全無效的註解（僅特殊字符）
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_OnlySpecialChars_ReturnsDefault()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("@#$%^&*");
        Assert.Equal("UncommentedElement", result);
    }

    /// <summary>
    /// 測試空白或 null 註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_NullOrWhitespace_ReturnsDefault()
    {
        Assert.Equal("UncommentedElement", XmlCommentNode.ConvertCommentToElementName(""));
        Assert.Equal("UncommentedElement", XmlCommentNode.ConvertCommentToElementName("   "));
        Assert.Equal("UncommentedElement", XmlCommentNode.ConvertCommentToElementName("\t\n"));
    }

    /// <summary>
    /// 測試複雜的混合註解
    /// </summary>
    [Fact]
    public void ConvertCommentToElementName_ComplexMixedComment_ProducesValidName()
    {
        var result = XmlCommentNode.ConvertCommentToElementName("--- deprecated: use new-config.xml instead ---");
        // 預期：移除所有特殊字符和空格，結果應該是合法的名稱
        Assert.NotEmpty(result);
        Assert.DoesNotContain(" ", result);
        Assert.DoesNotContain("-", result);
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain(".", result);
    }

    /// <summary>
    /// 測試 XmlCommentNode 實例創建與基本屬性
    /// </summary>
    [Fact]
    public void XmlCommentNode_CreateInstance_HasCorrectProperties()
    {
        var root = new XElement("root");
        var comment = new XComment("test comment");
        root.Add(comment);

        var parentNode = XmlElementNode.FromElement(root);
        var commentNode = new XmlCommentNode(comment, parentNode);

        Assert.Equal("Comment", commentNode.NodeType);
        Assert.Equal("<!-- test comment -->", commentNode.DisplayLabel);
        Assert.Equal("test comment", commentNode.CommentText);
    }

    /// <summary>
    /// 測試編輯註解文字
    /// </summary>
    [Fact]
    public void XmlCommentNode_EditCommentText_UpdatesDisplayLabel()
    {
        var root = new XElement("root");
        var comment = new XComment("original");
        root.Add(comment);

        var parentNode = XmlElementNode.FromElement(root);
        var commentNode = new XmlCommentNode(comment, parentNode);

        commentNode.CommentText = "modified";

        Assert.Equal("<!-- modified -->", commentNode.DisplayLabel);
        Assert.Equal("modified", comment.Value);
    }

    /// <summary>
    /// 測試移除註解節點
    /// </summary>
    [Fact]
    public void XmlCommentNode_RemoveFromParent_RemovesCommentFromDocument()
    {
        var root = new XElement("root");
        var comment = new XComment("to remove");
        root.Add(comment);

        var parentNode = XmlElementNode.FromElement(root);
        var commentNode = parentNode.Children[0] as XmlCommentNode;
        Assert.NotNull(commentNode);

        commentNode.RemoveFromParent();

        // 驗證註解已從文檔移除
        Assert.Empty(root.Nodes().OfType<XComment>());
    }

    /// <summary>
    /// 測試含多個元素和註解的複雜 XML 結構
    /// </summary>
    [Fact]
    public void XmlElementNode_WithMixedCommentsAndElements_MaintainsOrder()
    {
        var root = new XElement("root");
        root.Add(new XComment("First comment"));
        root.Add(new XElement("element1"));
        root.Add(new XComment("Second comment"));
        root.Add(new XElement("element2"));

        var parentNode = XmlElementNode.FromElement(root);

        Assert.Equal(4, parentNode.Children.Count);
        Assert.IsType<XmlCommentNode>(parentNode.Children[0]);
        Assert.IsType<XmlElementNode>(parentNode.Children[1]);
        Assert.IsType<XmlCommentNode>(parentNode.Children[2]);
        Assert.IsType<XmlElementNode>(parentNode.Children[3]);

        var comment1 = parentNode.Children[0] as XmlCommentNode;
        Assert.Equal("First comment", comment1?.CommentText);

        var comment2 = parentNode.Children[2] as XmlCommentNode;
        Assert.Equal("Second comment", comment2?.CommentText);
    }

    [Fact]
    public void AddChildElement_FromParsedXml_PreservesAttributesAndName()
    {
        var root = new XElement("configSections");
        var parentNode = XmlElementNode.FromElement(root);

        var xElement = XElement.Parse(
            "<section name=\"sustainsys.saml2\" type=\"Sustainsys.Saml2.Configuration.SustainsysSaml2Section, Sustainsys.Saml2\" />");
        parentNode.AddChildElement(xElement);

        var added = root.Elements().First();
        Assert.Equal("section", added.Name.LocalName);
        Assert.Equal("sustainsys.saml2", added.Attribute("name")!.Value);
        Assert.Equal("Sustainsys.Saml2.Configuration.SustainsysSaml2Section, Sustainsys.Saml2", added.Attribute("type")!.Value);
    }
}

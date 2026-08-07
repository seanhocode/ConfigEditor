using ConfigEditor.Core.Services;
using System.Xml.Linq;

namespace ConfigEditor.Infrastructure.Services;

/// <summary>
/// 以 XML 結構定位邏輯實作批次目標套用 Port
/// </summary>
public class XmlService : IXmlService
{
    /// <summary>
    /// 載入 XML 文件並保留空白格式
    /// </summary>
    /// <param name="filePath">XML 檔案路徑</param>
    /// <returns>載入後文件</returns>
    public XDocument LoadXDocument(string filePath)
    {
        return XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
    }

    /// <summary>
    /// 將 XML 文件寫回檔案
    /// </summary>
    /// <param name="filePath">目標檔案路徑</param>
    /// <param name="document">欲儲存文件</param>
    public void SaveXDocument(string filePath, XDocument document)
    {
        document.Save(filePath);
    }

}

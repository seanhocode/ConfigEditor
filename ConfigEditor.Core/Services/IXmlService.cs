using System.Xml.Linq;

namespace ConfigEditor.Core.Services{
    public interface IXmlService
    {
        /// <summary>
        /// 載入指定 XML 檔案
        /// </summary>
        /// <param name="filePath">XML 檔案路徑</param>
        /// <returns>載入後文件</returns>
        XDocument LoadXDocument(string filePath);

        /// <summary>
        /// 儲存 XML 到指定檔案
        /// </summary>
        /// <param name="filePath">目標檔案路徑</param>
        /// <param name="document">欲儲存文件</param>
        void SaveXDocument(string filePath, XDocument document);
    }
}
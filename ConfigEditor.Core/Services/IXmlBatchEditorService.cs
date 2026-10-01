using ConfigEditor.Core.Models.XmlBatchEditorService;

namespace ConfigEditor.Core.Services
{
    public interface IXmlBatchEditorService
    {

        /// <summary>
        /// 針對多個檔案分析可共同編輯的目標集合
        /// </summary>
        /// <param name="filePaths">待分析檔案路徑</param>
        /// <returns>共同可編輯目標清單</returns>
        IReadOnlyList<BatchTarget> AnalyzeBatchTargets(IEnumerable<string> filePaths);

        /// <summary>
        /// 將指定目標套用新值到單一檔案
        /// </summary>
        /// <param name="filePath">目標檔案路徑</param>
        /// <param name="target">欲更新的批次目標</param>
        /// <param name="newValue">新值</param>
        /// <returns>實際更新筆數</returns>
        int ApplyBatchTarget(string filePath, BatchTarget target, string newValue);
    }
}

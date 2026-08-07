using ConfigEditor.Core.Enums;

namespace ConfigEditor.Core.Models.XmlBatchEditorService
{
    /// <summary>
    /// 描述可批次更新目標的核心契約
    /// </summary>
    /// <param name="TargetKey">用於精準比對的唯一鍵</param>
    /// <param name="TargetSelector">目標元素選擇器</param>
    /// <param name="ValueSelector">目標值選擇器（例如 @value、text()）</param>
    /// <param name="Kind">目標值型別</param>
    /// <param name="SampleValue">分析時擷取的範例值</param>
    public sealed record BatchTarget(
        string TargetKey,
        string TargetSelector,
        string ValueSelector,
        BatchTargetKind Kind,
        string SampleValue);

}

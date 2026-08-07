using ConfigEditor.Core.Enums;

namespace ConfigEditor.Core.Models.XmlBatchEditorService
{
    /// <summary>
    /// Xml 單一可編輯節點
    /// </summary>
    public sealed class EditableNode
    {
        /// <summary>
        /// 欄位唯一鍵（TargetSelector|ValueSelector）
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// 目標元素選擇器
        /// </summary>
        public string TargetSelector { get; init; } = string.Empty;

        /// <summary>
        /// 目標值選擇器
        /// </summary>
        public string ValueSelector { get; init; } = string.Empty;

        /// <summary>
        /// 目標值型別
        /// </summary>
        public BatchTargetKind Kind { get; init; }

        /// <summary>
        /// 範例值
        /// </summary>
        public string SampleValue { get; init; } = string.Empty;
    }
}

using ConfigEditor.Core.Enums;

namespace ConfigEditor.Core.Models
{
    /// <summary>
    /// 批次編輯目標的 UI 模型
    /// </summary>
    public class BatchEditTarget
    {
        /// <summary>
        /// 用於精準定位的唯一鍵
        /// </summary>
        public string TargetKey { get; init; } = string.Empty;

        /// <summary>
        /// 目標元素選擇器
        /// </summary>
        public string TargetSelector { get; init; } = string.Empty;

        /// <summary>
        /// 目標值選擇器（例如 @value、text()）
        /// </summary>
        public string ValueSelector { get; init; } = string.Empty;

        /// <summary>
        /// 值型別
        /// </summary>
        public BatchTargetKind Kind { get; init; }

        /// <summary>
        /// 目標路徑（相容舊顯示欄位）
        /// </summary>
        public string Path { get; init; } = string.Empty;

        /// <summary>
        /// 定位屬性名稱（相容舊欄位）
        /// </summary>
        public string? LocatorAttributeName { get; init; }

        /// <summary>
        /// 定位屬性值（相容舊欄位）
        /// </summary>
        public string? LocatorAttributeValue { get; init; }

        /// <summary>
        /// 目標屬性名稱（屬性型目標時使用）
        /// </summary>
        public string? TargetAttributeName { get; init; }

        /// <summary>
        /// 供畫面顯示的範例值
        /// </summary>
        public string SampleValue { get; init; } = string.Empty;

        /// <summary>
        /// 顯示用型別文字
        /// </summary>
        public string DisplayType => Kind == BatchTargetKind.Attribute ? "屬性" : "節點文字";

        /// <summary>
        /// 顯示用完整目標路徑
        /// </summary>
        public string DisplayPath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TargetSelector) && !string.IsNullOrWhiteSpace(ValueSelector))
                {
                    return $"{TargetSelector}/{ValueSelector}";
                }

                var basePath = string.IsNullOrWhiteSpace(LocatorAttributeName)
                    ? Path
                    : $"{Path}[@{LocatorAttributeName}='{LocatorAttributeValue}']";

                if (Kind == BatchTargetKind.Attribute)
                {
                    return $"{basePath}/@{TargetAttributeName}";
                }

                return $"{basePath}/text()";
            }
        }
    }
}

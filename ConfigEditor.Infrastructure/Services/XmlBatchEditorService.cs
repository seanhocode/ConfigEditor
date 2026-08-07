using ConfigEditor.Core.Enums;
using ConfigEditor.Core.Models.XmlBatchEditorService;
using ConfigEditor.Core.Services;
using System.Xml.Linq;

namespace ConfigEditor.Infrastructure.Services
{
    public class XmlBatchEditorService : IXmlBatchEditorService
    {
        /// <summary>
        /// Xml 識別屬性
        /// </summary>
        /// <remarks>識別屬性不可編輯</remarks>
        private static readonly string[] _IdentityAttributeNameList = ["id", "name", "key", "tag", "target", "type", "ref"];

        /// <summary>
        /// Xml 元素直接文字節點的選擇器
        /// </summary>
        private static readonly string _ElementTextSelector = "text()";

        private readonly IXmlService _xmlService;

        public XmlBatchEditorService(IXmlService xmlService)
        {
            _xmlService = xmlService;
        }

        /// <summary>
        /// 分析輸入檔案中共同存在的可編輯目標
        /// </summary>
        /// <param name="filePaths">待分析檔案路徑</param>
        /// <returns>共同目標契約清單</returns>
        public IReadOnlyList<BatchTarget> AnalyzeBatchTargets(IEnumerable<string> batchFileList)
        {
            IList<string> normalizedFiles = batchFileList
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedFiles.Count == 0)
            {
                return [];
            }

            Dictionary<string, int> counter = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, EditableNode> sampleSlots = new Dictionary<string, EditableNode>(StringComparer.Ordinal);

            foreach (var filePath in normalizedFiles)
            {
                IReadOnlyList<EditableNode> xmlEditableNodeList = GetXmlEditableNodeList(filePath);
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (EditableNode xmlNode in xmlEditableNodeList)
                {
                    if (!seen.Add(xmlNode.Key))
                    {
                        continue;
                    }

                    counter[xmlNode.Key] = counter.GetValueOrDefault(xmlNode.Key) + 1;
                    sampleSlots.TryAdd(xmlNode.Key, xmlNode);
                }
            }

            return counter
                .Where(item => item.Value == normalizedFiles.Count)
                .Select(item => sampleSlots[item.Key])
                .Select(slot => new BatchTarget(
                    slot.Key,
                    slot.TargetSelector,
                    slot.ValueSelector,
                    slot.Kind,
                    slot.SampleValue))
                .OrderBy(target => target.TargetSelector, StringComparer.OrdinalIgnoreCase)
                .ThenBy(target => target.ValueSelector, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 將指定目標套用新值到單一檔案
        /// </summary>
        /// <param name="filePath">目標檔案路徑</param>
        /// <param name="target">欲更新目標</param>
        /// <param name="newValue">新值</param>
        /// <returns>實際更新筆數</returns>
        public int ApplyBatchTarget(string filePath, BatchTarget target, string newValue)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            var doc = _xmlService.LoadXDocument(filePath);
            if (doc.Root is null)
            {
                return 0;
            }

            var normalizedNewValue = newValue ?? string.Empty;
            var updatedCount = 0;

            if (!string.IsNullOrWhiteSpace(target.TargetKey))
            {
                updatedCount = ApplyByTargetKey(doc, target.TargetKey, normalizedNewValue);
            }

            if (updatedCount == 0)
            {
                updatedCount = ApplyByLegacyTarget(doc, target, normalizedNewValue);
            }

            if (updatedCount > 0)
            {
                doc.Save(filePath);
            }

            return updatedCount;
        }

        private int ApplyByTargetKey(XDocument doc, string targetKey, string newValue)
        {
            var updatedCount = 0;
            foreach (var element in doc.Root!.DescendantsAndSelf())
            {
                var targetSelector = GetElementSelector(element);
                var identityNames = GetIdentityAttributeNames(element);

                foreach (var attribute in element.Attributes())
                {
                    if (identityNames.Contains(attribute.Name.LocalName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = GetNodeKey(targetSelector, $"@{attribute.Name.LocalName}");
                    if (!key.Equals(targetKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (attribute.Value.Equals(newValue, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    attribute.Value = newValue;
                    updatedCount++;
                }

                var directText = GetDirectElementText(element);
                if (!string.IsNullOrWhiteSpace(directText))
                {
                    var textKey = GetNodeKey(targetSelector, _ElementTextSelector);
                    if (!textKey.Equals(targetKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (directText.Equals(newValue, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var textNodes = element.Nodes().OfType<XText>().ToList();
                    foreach (var textNode in textNodes)
                    {
                        textNode.Remove();
                    }

                    element.AddFirst(new XText(newValue));
                    updatedCount++;
                }
            }

            return updatedCount;
        }

        private int ApplyByLegacyTarget(XDocument doc, BatchTarget target, string newValue)
        {
            if (doc.Root is null)
            {
                return 0;
            }

            var updatedCount = 0;
            var segments = target.TargetSelector
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (segments.Count == 0)
            {
                return 0;
            }

            var candidates = doc.Root
                .DescendantsAndSelf()
                .Where(element => element.Name.LocalName.Equals(segments[^1], StringComparison.OrdinalIgnoreCase));

            if (target.Kind == BatchTargetKind.Attribute)
            {
                var targetAttributeName = target.ValueSelector.TrimStart('@');
                if (string.IsNullOrWhiteSpace(targetAttributeName))
                {
                    return 0;
                }

                foreach (var element in candidates)
                {
                    var attribute = element.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals(targetAttributeName, StringComparison.OrdinalIgnoreCase));
                    if (attribute is null || attribute.Value.Equals(newValue, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    attribute.Value = newValue;
                    updatedCount++;
                }

                return updatedCount;
            }

            foreach (var element in candidates)
            {
                var directText = GetDirectElementText(element);
                if (string.IsNullOrWhiteSpace(directText) || directText.Equals(newValue, StringComparison.Ordinal))
                {
                    continue;
                }

                var textNodes = element.Nodes().OfType<XText>().ToList();
                foreach (var textNode in textNodes)
                {
                    textNode.Remove();
                }

                element.AddFirst(new XText(newValue));
                updatedCount++;
            }

            return updatedCount;
        }

        /// <summary>
        /// 取得指定 element 的完整路徑選擇器
        /// </summary>
        /// <param name="element"></param>
        /// <remarks>
        /// element 的完整路徑選擇器組成方式: 
        /// </remarks>
        /// <returns></returns>
        private string GetElementSelector(XElement element)
        {
            IList<string> segments = element
                .AncestorsAndSelf() //取得當前 element 及其所有上層祖先 element
                .Select(GetSingleElementSegment)
                .ToList();

            return "/" + string.Join('/', segments);
        }

        /// <summary>
        /// 取得單個 element 的路徑片段
        /// </summary>
        /// <param name="element">要建構路徑片段的 XML 元素</param>
        /// <remarks>
        /// element 的路徑片段組成方式:
        /// <list type="number">
        ///   <item>若 element 擁有 identity attribute（定義於 IdentityAttributeNameList），
        ///         則以 <c>elementName[@id='v1' and @name='v2']</c> 格式組成</item>
        ///   <item>若無 identity attribute 且有父節點，則以同名兄弟元素的索引定位，
        ///         格式為 <c>elementName[index]</c></item>
        ///   <item>若無 identity attribute 且無父節點（根元素），則直接回傳 element name</item>
        /// </list>
        /// </remarks>
        /// <returns>該 element 的路徑片段字串</returns>
        private string GetSingleElementSegment(XElement element)
        {
            string elementIdentitySegment = element.Name.LocalName;
            HashSet<string> identityAttributeNameSet = GetIdentityAttributeNames(element);

            // element 的 identity attribute 修飾詞清單，格式為 @attributeName='attributeValue'，若有多個則以 and 連接
            IList<string> qualifiers = new List<string>();

            //這裡不使用 identityAttributeNameSet 遍歷是為了保持 IdentityAttributeNameList 的順序
            foreach (string identityName in _IdentityAttributeNameList)
            {
                //如果當前的 global identity attribute name 不在 element 的 identity attribute name set 則跳過
                if (!identityAttributeNameSet.Contains(identityName, StringComparer.OrdinalIgnoreCase)) { continue; }

                XAttribute identityAttribute =
                        element.Attributes().First(attribute =>
                            attribute.Name.LocalName.Equals(identityName, StringComparison.OrdinalIgnoreCase));

                qualifiers.Add($"@{identityAttribute.Name.LocalName}='{EscapeSelectorValue(identityAttribute.Value)}'");
            }

            if (qualifiers.Count > 0)
            {
                return elementIdentitySegment + "[" + string.Join(" and ", qualifiers) + "]";
            }

            //如果沒有 identity attribute name 且 element 有父節點，則使用 index 方式定位
            if (element.Parent is not null)
            {
                int index = element.Parent.Elements(element.Name).TakeWhile(node => node != element).Count() + 1;
                return $"{elementIdentitySegment}[{index}]";
            }

            // 如果沒有 identity attribute name 且 element 沒有父節點，則直接回傳 element name
            return elementIdentitySegment;
        }

        /// <summary>
        /// 取得該 Element 中的 Identity Attribute Name Set
        /// </summary>
        /// <param name="element">要檢查的 XML 元素</param>
        /// <remarks>是否為 Identity 定義在 IdentityAttributeName，且屬性值不可為空白</remarks>
        /// <returns>回傳傳入 Element 中，Attribute.Name.LocalName 符合 IdentityAttributeName(忽略大小寫) 且值非空白的 Attribute.Name.LocalName HashSet</returns>
        private HashSet<string> GetIdentityAttributeNames(XElement element)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string priorityName in _IdentityAttributeNameList)
            {
                XAttribute? attribute =
                    element.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals(priorityName, StringComparison.OrdinalIgnoreCase));

                if (attribute is null || string.IsNullOrWhiteSpace(attribute.Value))
                {
                    continue;
                }

                result.Add(attribute.Name.LocalName);
            }

            return result;
        }

        /// <summary>
        /// 取得 EditableNode 的唯一鍵
        /// </summary>
        /// <param name="targetSelector"></param>
        /// <param name="valueSelector"></param>
        /// <remarks>組合 targetSelector 與 valueSelector 為唯一鍵</remarks>
        /// <returns></returns>
        private string GetNodeKey(string targetSelector, string valueSelector)
        {
            return $"{targetSelector}|{valueSelector}";
        }

        /// <summary>
        /// 取得指定 element 的直接文字內容（不包含子元素的文字）
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private string GetDirectElementText(XElement element)
        {
            return string.Concat(element.Nodes().OfType<XText>().Select(node => node.Value)).Trim();
        }

        private string EscapeSelectorValue(string value)
        {
            return value.Replace("'", "&apos;");
        }

        /// <summary>
        /// 取得指定 XML 檔案中所有可編輯節點的清單
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>返回唯獨</returns>
        private IReadOnlyList<EditableNode> GetXmlEditableNodeList(string filePath)
        {
            XDocument doc = _xmlService.LoadXDocument(filePath);
            if (doc.Root is null) { return []; }

            List<EditableNode> result = new List<EditableNode>();

            //取得xml文件中所有element，包含根元素
            IList<XElement> elements = doc.Root.DescendantsAndSelf().ToList();

            foreach (XElement element in elements)
            {
                string targetSelector = GetElementSelector(element);
                HashSet<string> identityNames = GetIdentityAttributeNames(element);

                //找出所有 element 的 attribute，並將非 identity attribute 轉換為 Attribute Type EditableSlot
                foreach (XAttribute attribute in element.Attributes())
                {
                    //如果是 identityAttribute(不可編輯) 則跳過
                    if (identityNames.Contains(attribute.Name.LocalName, StringComparer.OrdinalIgnoreCase)) { continue; }

                    string valueSelector = $"@{attribute.Name.LocalName}";
                    result.Add(new EditableNode
                    {
                        Key = GetNodeKey(targetSelector, valueSelector),
                        TargetSelector = targetSelector,
                        ValueSelector = valueSelector,
                        Kind = BatchTargetKind.Attribute,
                        SampleValue = attribute.Value,
                    });
                }

                //找出 element 的直接文字節點，並將其轉換為 ElementText Type EditableSlot
                string elementText = GetDirectElementText(element);
                if (!string.IsNullOrWhiteSpace(elementText))
                {
                    result.Add(new EditableNode
                    {
                        Key = GetNodeKey(targetSelector, _ElementTextSelector),
                        TargetSelector = targetSelector,
                        ValueSelector = _ElementTextSelector,
                        Kind = BatchTargetKind.ElementText,
                        SampleValue = elementText,
                    });
                }
            }

            return result;
        }
    }
}

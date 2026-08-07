using ConfigEditor.Infrastructure.Services;
using System.Xml.Linq;

namespace ConfigEditor.Infrastructure.Tests;

public class WorkflowE2ETests
{
    [Fact]
    public void EndToEnd_Workflow_OnTestData_Succeeds()
    {
        XmlService xmlService = new XmlService();
        XmlBatchEditorService xmlBatchEditorService = new XmlBatchEditorService(xmlService);

        using var factory = new TestFileFactory();
        var allFiles = factory.CreateDummyConfigFiles(55, 18);
        Assert.Equal(73, allFiles.Count);

        VerifyAttributeValueTypePair(xmlBatchEditorService);
        VerifyNestedStructureTypePair(xmlBatchEditorService);
        VerifyElementTextTypePair(xmlBatchEditorService);

        VerifyLoadSaveRoundtrip(xmlService);
    }

    private static void VerifyAttributeValueTypePair(XmlBatchEditorService xmlBatchEditorService)
    {
        using var factory = new TestFileFactory();
        var (file1, file2) = factory.CreateXmlFilePair();

        var targets = xmlBatchEditorService.AnalyzeBatchTargets([file1, file2]);
        var target = targets.First(item => item.ValueSelector.Equals("@Value", StringComparison.OrdinalIgnoreCase));

        var updated1 = xmlBatchEditorService.ApplyBatchTarget(file1, target, "e2e-attr-value");
        var updated2 = xmlBatchEditorService.ApplyBatchTarget(file2, target, "e2e-attr-value");

        Assert.Equal(1, updated1);
        Assert.Equal(1, updated2);
    }

    private static void VerifyNestedStructureTypePair(XmlBatchEditorService xmlBatchEditorService)
    {
        using var factory = new TestFileFactory();
        var (file1, file2) = factory.CreateConfigFilePair();

        var targets = xmlBatchEditorService.AnalyzeBatchTargets([file1, file2]);
        var target = targets.First(item => item.TargetSelector.Contains("replace", StringComparison.OrdinalIgnoreCase)
            && item.ValueSelector.Equals("@value", StringComparison.OrdinalIgnoreCase));

        var updated1 = xmlBatchEditorService.ApplyBatchTarget(file1, target, "e2e-nested-value");
        var updated2 = xmlBatchEditorService.ApplyBatchTarget(file2, target, "e2e-nested-value");

        Assert.Equal(1, updated1);
        Assert.Equal(1, updated2);
    }

    private static void VerifyElementTextTypePair(XmlBatchEditorService xmlBatchEditorService)
    {
        using var factory = new TestFileFactory();
        var file1 = factory.CreateFile("a.xml", "<root><item key='x'><value>old-a</value></item></root>");
        var file2 = factory.CreateFile("b.xml", "<root><item key='x'><value>old-b</value></item></root>");

        var targets = xmlBatchEditorService.AnalyzeBatchTargets([file1, file2]);
        var target = targets.First(item => item.ValueSelector.Equals("text()", StringComparison.OrdinalIgnoreCase)
            && item.TargetSelector.Contains("value", StringComparison.OrdinalIgnoreCase));

        var updated1 = xmlBatchEditorService.ApplyBatchTarget(file1, target, "e2e-text-value");
        var updated2 = xmlBatchEditorService.ApplyBatchTarget(file2, target, "e2e-text-value");

        Assert.Equal(1, updated1);
        Assert.Equal(1, updated2);

        var reloaded1 = XDocument.Load(file1, LoadOptions.PreserveWhitespace);
        var reloaded2 = XDocument.Load(file2, LoadOptions.PreserveWhitespace);
        Assert.Equal("e2e-text-value", reloaded1.Root!.Element("item")!.Element("value")!.Value);
        Assert.Equal("e2e-text-value", reloaded2.Root!.Element("item")!.Element("value")!.Value);
    }

    private static void VerifyLoadSaveRoundtrip(XmlService xmlService)
    {
        using var factory = new TestFileFactory();
        var file = factory.CreateXmlFilePair().file1Xml;

        var doc = xmlService.LoadXDocument(file);
        doc.Root!.SetAttributeValue("e2e-check", "ok");
        xmlService.SaveXDocument(file, doc);

        var verify = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        Assert.Equal("ok", verify.Root!.Attribute("e2e-check")!.Value);
    }
}

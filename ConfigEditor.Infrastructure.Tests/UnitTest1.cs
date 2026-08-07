using ConfigEditor.Infrastructure.Services;
using System.Xml.Linq;

namespace ConfigEditor.Infrastructure.Tests;

public class AdapterIntegrationTests
{
    [Fact]
    public void FileSystemConfigDiscoveryAdapter_ScansConfigAndXmlFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            var nested = Path.Combine(root, "a", "b");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(root, "one.config"), "<c />");
            File.WriteAllText(Path.Combine(nested, "two.xml"), "<x />");
            File.WriteAllText(Path.Combine(nested, "ignore.txt"), "na");

            var configService = new ConfigService();
            var files = configService.ScanFolderConfig(root, whitelistEnabled: false, whitelistItems: []);

            Assert.Equal(2, files.Count);
            Assert.Contains(files, path => path.EndsWith("one.config", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(files, path => path.EndsWith("two.xml", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public void XmlBatchTargetAnalyzerAdapter_KnownPairs_MatchBaselineCounts()
    {
        using var factory = new TestFileFactory();
        XmlBatchEditorService xmlBatchEditorService = new XmlBatchEditorService(new XmlService());

        var (xmlFile1, xmlFile2) = factory.CreateXmlFilePair();
        var (configFile1, configFile2) = factory.CreateConfigFilePair();

        var xmlTargets = xmlBatchEditorService.AnalyzeBatchTargets([xmlFile1, xmlFile2]);
        var configTargets = xmlBatchEditorService.AnalyzeBatchTargets([configFile1, configFile2]);

        Assert.Equal(25, xmlTargets.Count);
        Assert.Equal(10, configTargets.Count);
    }

    [Fact]
    public void XmlBatchApplyAdapter_UpdatesAttributeValueByTargetKey()
    {
        var root = CreateTempDirectory();
        var file = Path.Combine(root, "sample.xml");
        File.WriteAllText(file, "<root><item key='k1' value='old' /></root>");

        try
        {
            XmlBatchEditorService xmlBatchEditorService = new XmlBatchEditorService(new XmlService());
            var target = xmlBatchEditorService
                .AnalyzeBatchTargets([file])
                .First(item => item.TargetSelector.Contains("item", StringComparison.OrdinalIgnoreCase)
                    && item.ValueSelector.Equals("@value", StringComparison.OrdinalIgnoreCase));

            var updated = xmlBatchEditorService.ApplyBatchTarget(file, target, "new");
            var doc = XDocument.Load(file, LoadOptions.PreserveWhitespace);
            var value = doc.Root!.Element("item")!.Attribute("value")!.Value;

            Assert.Equal(1, updated);
            Assert.Equal("new", value);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public void XmlDocumentPortAdapter_LoadAndSave_RoundtripWorks()
    {
        var root = CreateTempDirectory();
        var file = Path.Combine(root, "doc.xml");
        File.WriteAllText(file, "<root><a>1</a></root>");

        try
        {
            var xmlService = new XmlService();
            var doc = xmlService.LoadXDocument(file);
            doc.Root!.Element("a")!.Value = "2";
            xmlService.SaveXDocument(file, doc);

            var saved = XDocument.Load(file, LoadOptions.PreserveWhitespace);
            Assert.Equal("2", saved.Root!.Element("a")!.Value);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ConfigEditor.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
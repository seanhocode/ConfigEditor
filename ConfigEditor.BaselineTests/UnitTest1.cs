using ConfigEditor.Infrastructure.Services;

namespace ConfigEditor.BaselineTests;

public class RefactorBaselineTests
{
    [Fact]
    public void Scan_TemporaryFolder_ReturnsExpectedCountAndOrder()
    {
        using var factory = new TestFileFactory();
        var configService = new ConfigService();

        var files = factory.CreateDummyConfigFiles(55, 18);
        var scanResults = configService.ScanFolderConfig(factory.TempDirectory, whitelistEnabled: false, whitelistItems: []);

        Assert.Equal(73, scanResults.Count);

        var expectedOrder = scanResults
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expectedOrder, scanResults);
    }

    [Fact]
    public void AnalyzeCommonTargets_XmlConfigPairs_MatchesBaselineCounts()
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
    public void ApplyBatchTarget_OnTempCopies_UpdatesOneEntryPerFile()
    {
        using var factory = new TestFileFactory();
        XmlBatchEditorService xmlBatchEditorService = new XmlBatchEditorService(new XmlService());

        var (xmlFile1, xmlFile2) = factory.CreateXmlFilePair();
        var (configFile1, configFile2) = factory.CreateConfigFilePair();

        var xmlTarget = xmlBatchEditorService.AnalyzeBatchTargets([xmlFile1, xmlFile2])[0];
        var configTarget = xmlBatchEditorService.AnalyzeBatchTargets([configFile1, configFile2])[0];

        var xmlUpdated1 = xmlBatchEditorService.ApplyBatchTarget(xmlFile1, xmlTarget, "baseline-test-value");
        var xmlUpdated2 = xmlBatchEditorService.ApplyBatchTarget(xmlFile2, xmlTarget, "baseline-test-value");
        var configUpdated1 = xmlBatchEditorService.ApplyBatchTarget(configFile1, configTarget, "baseline-test-value");
        var configUpdated2 = xmlBatchEditorService.ApplyBatchTarget(configFile2, configTarget, "baseline-test-value");

        Assert.Equal(1, xmlUpdated1);
        Assert.Equal(1, xmlUpdated2);
        Assert.Equal(1, configUpdated1);
        Assert.Equal(1, configUpdated2);
    }
}



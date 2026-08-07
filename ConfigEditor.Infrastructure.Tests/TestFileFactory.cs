namespace ConfigEditor.Infrastructure.Tests;

/// <summary>
/// 測試檔案工廠，用於生成和管理臨時測試檔案
/// </summary>
public class TestFileFactory : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _createdFiles = [];

    public TestFileFactory()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"ConfigEditorTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 獲取臨時目錄路徑
    /// </summary>
    public string TempDirectory => _tempDirectory;

    /// <summary>
    /// 建立指定內容的檔案
    /// </summary>
    /// <param name="fileName">檔案名稱</param>
    /// <param name="content">檔案內容</param>
    /// <returns>完整的檔案路徑</returns>
    public string CreateFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
        _createdFiles.Add(filePath);
        return filePath;
    }

    /// <summary>
    /// 建立 XML 檔案對
    /// </summary>
    /// <returns>包含兩個檔案路徑的元組 (file1Xml, file2Xml)</returns>
    public (string file1Xml, string file2Xml) CreateXmlFilePair()
    {
        var file1 = CreateFile("module1.xml", TestResources.CustomizeModule1Xml);
        var file2 = CreateFile("module2.xml", TestResources.CustomizeModule2Xml);
        return (file1, file2);
    }

    /// <summary>
    /// 建立 Config 檔案對
    /// </summary>
    /// <returns>包含兩個檔案路徑的元組 (file1Config, file2Config)</returns>
    public (string file1Config, string file2Config) CreateConfigFilePair()
    {
        var file1 = CreateFile("module1.config", TestResources.CustomizeModule1Config);
        var file2 = CreateFile("module2.config", TestResources.CustomizeModule2Config);
        return (file1, file2);
    }

    /// <summary>
    /// 建立簡單的元素文字測試檔案
    /// </summary>
    /// <returns>檔案路徑</returns>
    public string CreateSimpleElementTextFile(string fileName = "simple.xml")
    {
        return CreateFile(fileName, TestResources.SimpleElementTextXml);
    }

    /// <summary>
    /// 建立指定數量的虛擬檔案以模擬檔案掃描
    /// </summary>
    /// <param name="configCount">Config 檔案數量（預設 55）</param>
    /// <param name="xmlCount">XML 檔案數量（預設 18）</param>
    /// <returns>所有建立的檔案路徑列表</returns>
    public List<string> CreateDummyConfigFiles(int configCount = 55, int xmlCount = 18)
    {
        var files = new List<string>();

        for (int i = 1; i <= configCount; i++)
        {
            var fileName = $"Config_{i:D3}.config";
            var content = TestResources.GenerateDummyConfig(10);
            files.Add(CreateFile(fileName, content));
        }

        for (int i = 1; i <= xmlCount; i++)
        {
            var fileName = $"Config_{configCount + i:D3}.xml";
            var content = TestResources.GenerateDummyConfig(10);
            files.Add(CreateFile(fileName, content));
        }

        return files;
    }

    /// <summary>
    /// 釋放資源並刪除所有臨時檔案
    /// </summary>
    public void Dispose()
    {
        try
        {
            foreach (var file in _createdFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // 忽略清理過程中的異常
        }
    }
}

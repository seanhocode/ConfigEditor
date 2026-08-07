using ConfigEditor.Core.Services;

namespace ConfigEditor.Infrastructure.Services;

/// <summary>
/// 掃描設定檔清單的用例實作
/// </summary>
public class ConfigService : IConfigService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".config",
        ".xml",
    };

    private static readonly HashSet<string> SkipDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "log",
        "logs",
        "LogFiles",
        "bin",
        "obj",
        "node_modules",
        ".git",
    };

    /// <summary>
    /// 依白名單模式或根目錄遞迴掃描 config/xml 檔案
    /// </summary>
    /// <param name="rootDirectory">預設掃描根目錄</param>
    /// <param name="whitelistEnabled">是否啟用白名單模式</param>
    /// <param name="whitelistItems">白名單檔案或資料夾清單</param>
    /// <returns>排序後的檔案絕對路徑清單</returns>
    public IReadOnlyList<string> ScanFolderConfig(string rootDirectory, bool whitelistEnabled, IEnumerable<string> whitelistItems)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (whitelistEnabled)
        {
            foreach (var item in whitelistItems)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                if (File.Exists(item))
                {
                    AddFileIfSupported(result, item);
                    continue;
                }

                if (Directory.Exists(item))
                {
                    AddFromDirectory(result, item);
                }
            }
        }
        else if (Directory.Exists(rootDirectory))
        {
            AddFromDirectory(result, rootDirectory);
        }

        return result.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddFromDirectory(HashSet<string> files, string directory)
    {
        var pending = new Stack<string>();
        pending.Push(directory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            if (ShouldSkipDirectory(current))
            {
                continue;
            }

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(current, "*", new EnumerationOptions
                {
                    RecurseSubdirectories = false,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System,
                    ReturnSpecialDirectories = false,
                });
            }
            catch
            {
                // Skip folders that are not accessible.
                continue;
            }

            foreach (var entry in entries)
            {
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    pending.Push(entry);
                    continue;
                }

                AddFileIfSupported(files, entry);
            }
        }
    }

    private static bool ShouldSkipDirectory(string path)
    {
        var name = Path.GetFileName(path);
        if (SkipDirectoryNames.Contains(name))
        {
            return true;
        }

        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                return true;
            }
        }
        catch
        {
            return true;
        }

        return false;
    }

    private static void AddFileIfSupported(HashSet<string> files, string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (SupportedExtensions.Contains(extension))
        {
            files.Add(Path.GetFullPath(filePath));
        }
    }
}

using ConfigEditor.Core.Enums;
using ConfigEditor.Core.Models;
using ConfigEditor.Core.Models.XmlBatchEditorService;
using ConfigEditor.Core.Services;
using ConfigEditor.Infrastructure.Services;
using ConfigEditor.Wpf.Infrastructure;
using ConfigEditor.Wpf.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;
using Forms = System.Windows.Forms;
using Win32 = Microsoft.Win32;

namespace ConfigEditor.Wpf.ViewModels;

/// <summary>
/// 主畫面 ViewModel，負責掃描、單檔編輯、批次分析與批次套用流程協調
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IConfigService _configService;
    private readonly IXmlService _xmlService;
    private readonly IXmlBatchEditorService _xmlBatchEditorService;
    private XDocument? _currentXmlDocument;
    private List<string> _allScannedFiles = new List<string>();

    private string _rootDirectory = string.Empty;
    private bool _isWhitelistEnabled;
    private bool _isScanning;
    private bool _isLiteUiMode;
    private FileTreeNode? _selectedTreeNode;
    private string _selectedFilePath = string.Empty;
    private string _editorStatus = "尚未掃描";
    private string _batchStatus = "尚未執行";
    private string _fileTreeSearchText = string.Empty;
    private string _batchNewValue = string.Empty;
    private string _batchTargetSearchText = string.Empty;
    private string _selectedBatchTargetType = "全部";
    private BatchEditTarget? _selectedBatchTarget;
    private string? _selectedWhitelistItem;
    private XmlElementNode? _selectedXmlNode;
    private XmlCommentNode? _selectedXmlComment;
    private XmlAttributeItem? _selectedXmlAttribute;
    private string _newChildElementName = "NewElement";
    private string _newChildTextValue = string.Empty;
    private string _newAttributeName = string.Empty;
    private string _newAttributeValue = string.Empty;
    private string _xmlSearchText = string.Empty;
    private XmlSearchResult? _selectedSearchResult;
    private bool _isPlainTextMode;
    private string _plainTextContent = string.Empty;

    /// <summary>
    /// 建立主畫面 ViewModel
    /// </summary>
    /// <param name="configService">設定服務</param>
    /// <param name="xmlService">XML 服務</param>
    /// <param name="xmlDocumentService">XML 文件服務</param>
    public MainViewModel(
        IConfigService configService,
        IXmlService xmlService,
        IXmlBatchEditorService xmlBatchEditorService)
    {
        _configService = configService;
        _xmlService = xmlService;
        _xmlBatchEditorService = xmlBatchEditorService;

        PickRootDirectoryCommand = new RelayCommand(_ => PickRootDirectory());
        ScanCommand = new RelayCommand(async _ => await ScanFilesAsync(), _ => !IsScanning);
        AddWhitelistFileCommand = new RelayCommand(_ => AddWhitelistFile());
        AddWhitelistFolderCommand = new RelayCommand(_ => AddWhitelistFolder());
        RemoveWhitelistItemCommand = new RelayCommand(_ => RemoveWhitelistItem(), _ => !string.IsNullOrWhiteSpace(SelectedWhitelistItem));
        SaveCurrentFileCommand = new RelayCommand(_ => SaveCurrentFile(), _ => !string.IsNullOrWhiteSpace(SelectedFilePath));
        AddSelectedFileToBatchCommand = new RelayCommand(_ => AddSelectedFileToBatch(), _ => !string.IsNullOrWhiteSpace(SelectedFilePath));
        AddCheckedTreeFilesToBatchCommand = new RelayCommand(_ => AddCheckedTreeFilesToBatch());
        AddCheckedFilesToBatchCommand = new RelayCommand(_ => AddCheckedFilesToBatch());
        RemoveBatchFileCommand = new RelayCommand(parameter => RemoveBatchFile(parameter as string));
        RefreshBatchSettingsCommand = new RelayCommand(_ => RefreshBatchSettings());
        FilterFileTreeCommand = new RelayCommand(_ => ApplyFileTreeFilter(expandAll: true));
        ClearFileTreeSearchCommand = new RelayCommand(_ => ClearFileTreeSearch());
        SearchBatchTargetsCommand = new RelayCommand(_ => ApplyBatchTargetFilters());
        ApplyBatchCommand = new RelayCommand(_ => ApplyBatch(), _ => BatchSelectedFiles.Count > 0 && SelectedBatchTarget is not null);
        AddChildNodeCommand = new RelayCommand(_ => AddChildNode(), _ => SelectedXmlNode is not null);
        DeleteSelectedNodeCommand = new RelayCommand(_ => DeleteSelectedNode(), _ => SelectedXmlNode is not null && !SelectedXmlNode.IsRoot);
        AddAttributeCommand = new RelayCommand(_ => AddAttribute(), _ => SelectedXmlNode is not null && !string.IsNullOrWhiteSpace(NewAttributeName));
        RemoveAttributeCommand = new RelayCommand(_ => RemoveAttribute(), _ => SelectedXmlNode is not null && SelectedXmlAttribute is not null);
        SearchXmlCommand = new RelayCommand(_ => SearchXmlNodes());
        ExpandAllXmlCommand = new RelayCommand(_ => ExpandAllXmlNodes());
        CollapseAllXmlCommand = new RelayCommand(_ => CollapseAllXmlNodes());
        DeleteCommentCommand = new RelayCommand(_ => DeleteSelectedComment(), _ => SelectedXmlComment is not null);
        UncommentCommand = new RelayCommand(_ => UncommentSelectedComment(), _ => SelectedXmlComment is not null);
    }

    /// <summary>
    /// 屬性變更事件
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 選擇掃描根目錄命令
    /// </summary>
    public ICommand PickRootDirectoryCommand { get; }

    /// <summary>
    /// 執行掃描命令
    /// </summary>
    public ICommand ScanCommand { get; }

    /// <summary>
    /// 新增白名單檔案命令
    /// </summary>
    public ICommand AddWhitelistFileCommand { get; }

    /// <summary>
    /// 新增白名單資料夾命令
    /// </summary>
    public ICommand AddWhitelistFolderCommand { get; }

    /// <summary>
    /// 移除白名單項目命令
    /// </summary>
    public ICommand RemoveWhitelistItemCommand { get; }

    /// <summary>
    /// 儲存目前 XML 命令
    /// </summary>
    public ICommand SaveCurrentFileCommand { get; }

    /// <summary>
    /// 將目前檔案加入批次命令
    /// </summary>
    public ICommand AddSelectedFileToBatchCommand { get; }

    /// <summary>
    /// 將樹狀勾選檔案加入批次命令
    /// </summary>
    public ICommand AddCheckedTreeFilesToBatchCommand { get; }

    /// <summary>
    /// 將可選清單勾選檔案加入批次命令
    /// </summary>
    public ICommand AddCheckedFilesToBatchCommand { get; }

    /// <summary>
    /// 移除批次檔案命令
    /// </summary>
    public ICommand RemoveBatchFileCommand { get; }

    /// <summary>
    /// 套用檔案樹搜尋命令
    /// </summary>
    public ICommand FilterFileTreeCommand { get; }

    /// <summary>
    /// 清除檔案樹搜尋命令
    /// </summary>
    public ICommand ClearFileTreeSearchCommand { get; }

    /// <summary>
    /// 分析共同批次目標命令
    /// </summary>
    public ICommand RefreshBatchSettingsCommand { get; }

    /// <summary>
    /// 篩選批次目標命令
    /// </summary>
    public ICommand SearchBatchTargetsCommand { get; }

    /// <summary>
    /// 套用批次更新命令
    /// </summary>
    public ICommand ApplyBatchCommand { get; }

    /// <summary>
    /// 新增子節點命令
    /// </summary>
    public ICommand AddChildNodeCommand { get; }

    /// <summary>
    /// 刪除選取節點命令
    /// </summary>
    public ICommand DeleteSelectedNodeCommand { get; }

    /// <summary>
    /// 新增屬性命令
    /// </summary>
    public ICommand AddAttributeCommand { get; }

    /// <summary>
    /// 移除屬性命令
    /// </summary>
    public ICommand RemoveAttributeCommand { get; }

    /// <summary>
    /// 搜尋 XML 節點命令
    /// </summary>
    public ICommand SearchXmlCommand { get; }

    /// <summary>
    /// 全展開 XML 命令
    /// </summary>
    public ICommand ExpandAllXmlCommand { get; }

    /// <summary>
    /// 收合 XML 命令
    /// </summary>
    public ICommand CollapseAllXmlCommand { get; }

    /// <summary>
    /// 刪除選取註解命令
    /// </summary>
    public ICommand DeleteCommentCommand { get; }

    /// <summary>
    /// 解除註解命令（將註解轉為可編輯元素）
    /// </summary>
    public ICommand UncommentCommand { get; }

    /// <summary>
    /// 白名單項目集合
    /// </summary>
    public ObservableCollection<string> WhitelistItems { get; } = [];

    /// <summary>
    /// 檔案樹節點集合
    /// </summary>
    public ObservableCollection<FileTreeNode> FileTreeNodes { get; } = [];

    /// <summary>
    /// XML 根節點集合
    /// </summary>
    public ObservableCollection<XmlElementNode> XmlRootNodes { get; } = [];

    /// <summary>
    /// XML 搜尋結果集合
    /// </summary>
    public ObservableCollection<XmlSearchResult> XmlSearchResults { get; } = [];

    /// <summary>
    /// 批次已選檔案集合
    /// </summary>
    public ObservableCollection<string> BatchSelectedFiles { get; } = [];

    /// <summary>
    /// 可勾選批次檔案集合
    /// </summary>
    public ObservableCollection<SelectableFileItem> BatchSelectableFiles { get; } = [];

    /// <summary>
    /// 批次目標類型選項
    /// </summary>
    public ObservableCollection<string> BatchTargetTypes { get; } = ["全部", "屬性", "節點文字"];

    /// <summary>
    /// 所有分析出的批次目標
    /// </summary>
    public ObservableCollection<BatchEditTarget> BatchAllTargets { get; } = [];

    /// <summary>
    /// 套用篩選後的批次目標
    /// </summary>
    public ObservableCollection<BatchEditTarget> BatchFilteredTargets { get; } = [];

    /// <summary>
    /// 掃描根目錄
    /// </summary>
    public string RootDirectory
    {
        get => _rootDirectory;
        set
        {
            if (_rootDirectory == value)
            {
                return;
            }

            _rootDirectory = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否啟用白名單模式
    /// </summary>
    public bool IsWhitelistEnabled
    {
        get => _isWhitelistEnabled;
        set
        {
            if (_isWhitelistEnabled == value)
            {
                return;
            }

            _isWhitelistEnabled = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否正在掃描檔案
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (_isScanning == value)
            {
                return;
            }

            _isScanning = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 是否啟用精簡 UI 模式（低規格機建議開啟）
    /// </summary>
    public bool IsLiteUiMode
    {
        get => _isLiteUiMode;
        set
        {
            if (_isLiteUiMode == value)
            {
                return;
            }

            _isLiteUiMode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目前選取的檔案樹節點
    /// </summary>
    public FileTreeNode? SelectedTreeNode
    {
        get => _selectedTreeNode;
        set
        {
            if (_selectedTreeNode == value)
            {
                return;
            }

            _selectedTreeNode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目前選取檔案路徑
    /// </summary>
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (_selectedFilePath == value)
            {
                return;
            }

            _selectedFilePath = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 單檔編輯狀態訊息
    /// </summary>
    public string EditorStatus
    {
        get => _editorStatus;
        set
        {
            if (_editorStatus == value)
            {
                return;
            }

            _editorStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 批次流程狀態訊息
    /// </summary>
    public string BatchStatus
    {
        get => _batchStatus;
        set
        {
            if (_batchStatus == value)
            {
                return;
            }

            _batchStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 批次套用的新值
    /// </summary>
    public string BatchNewValue
    {
        get => _batchNewValue;
        set
        {
            if (_batchNewValue == value)
            {
                return;
            }

            _batchNewValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 檔案樹搜尋關鍵字
    /// </summary>
    public string FileTreeSearchText
    {
        get => _fileTreeSearchText;
        set
        {
            if (_fileTreeSearchText == value)
            {
                return;
            }

            _fileTreeSearchText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 批次目標搜尋關鍵字
    /// </summary>
    public string BatchTargetSearchText
    {
        get => _batchTargetSearchText;
        set
        {
            if (_batchTargetSearchText == value)
            {
                return;
            }

            _batchTargetSearchText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目前選取的批次目標型別
    /// </summary>
    public string SelectedBatchTargetType
    {
        get => _selectedBatchTargetType;
        set
        {
            if (_selectedBatchTargetType == value)
            {
                return;
            }

            _selectedBatchTargetType = value;
            OnPropertyChanged();
            ApplyBatchTargetFilters();
        }
    }

    /// <summary>
    /// 目前選取的批次目標
    /// </summary>
    public BatchEditTarget? SelectedBatchTarget
    {
        get => _selectedBatchTarget;
        set
        {
            if (_selectedBatchTarget == value)
            {
                return;
            }

            _selectedBatchTarget = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 目前選取的白名單項目
    /// </summary>
    public string? SelectedWhitelistItem
    {
        get => _selectedWhitelistItem;
        set
        {
            if (_selectedWhitelistItem == value)
            {
                return;
            }

            _selectedWhitelistItem = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 目前選取的 XML 節點
    /// </summary>
    public XmlElementNode? SelectedXmlNode
    {
        get => _selectedXmlNode;
        set
        {
            if (_selectedXmlNode == value)
            {
                return;
            }

            _selectedXmlNode = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 目前選取的 XML 註解節點
    /// </summary>
    public XmlCommentNode? SelectedXmlComment
    {
        get => _selectedXmlComment;
        set
        {
            if (_selectedXmlComment == value)
            {
                return;
            }

            _selectedXmlComment = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 目前選取的 XML 屬性
    /// </summary>
    public XmlAttributeItem? SelectedXmlAttribute
    {
        get => _selectedXmlAttribute;
        set
        {
            if (_selectedXmlAttribute == value)
            {
                return;
            }

            _selectedXmlAttribute = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 新增子節點名稱
    /// </summary>
    public string NewChildElementName
    {
        get => _newChildElementName;
        set
        {
            if (_newChildElementName == value)
            {
                return;
            }

            _newChildElementName = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 新增子節點文字值
    /// </summary>
    public string NewChildTextValue
    {
        get => _newChildTextValue;
        set
        {
            if (_newChildTextValue == value)
            {
                return;
            }

            _newChildTextValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 新增屬性名稱
    /// </summary>
    public string NewAttributeName
    {
        get => _newAttributeName;
        set
        {
            if (_newAttributeName == value)
            {
                return;
            }

            _newAttributeName = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    /// <summary>
    /// 新增屬性值
    /// </summary>
    public string NewAttributeValue
    {
        get => _newAttributeValue;
        set
        {
            if (_newAttributeValue == value)
            {
                return;
            }

            _newAttributeValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// XML 搜尋關鍵字
    /// </summary>
    public string XmlSearchText
    {
        get => _xmlSearchText;
        set
        {
            if (_xmlSearchText == value)
            {
                return;
            }

            _xmlSearchText = value;
            OnPropertyChanged();
        }
    }

    public bool IsPlainTextMode
    {
        get => _isPlainTextMode;
        set
        {
            if (_isPlainTextMode == value) return;
            _isPlainTextMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotPlainTextMode));
            XmlSearchResults.Clear();
            SelectedSearchResult = null;
            if (value)
            {
                PlainTextContent = File.Exists(SelectedFilePath) ? File.ReadAllText(SelectedFilePath) : string.Empty;
                EditorStatus = "切換到純文字編輯模式";
            }
            else
            {
                LoadSelectedFile();
            }
        }
    }

    public bool IsNotPlainTextMode
    {
        get => !_isPlainTextMode;
        set => IsPlainTextMode = !value;
    }

    public string PlainTextContent
    {
        get => _plainTextContent;
        set
        {
            if (_plainTextContent == value) return;
            _plainTextContent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 目前選取搜尋結果
    /// </summary>
    public XmlSearchResult? SelectedSearchResult
    {
        get => _selectedSearchResult;
        set
        {
            if (_selectedSearchResult == value)
            {
                return;
            }

            _selectedSearchResult = value;
            OnPropertyChanged();

            if (_selectedSearchResult is not null && _selectedSearchResult.Node is not null)
            {
                FocusNode(_selectedSearchResult.Node);
            }
        }
    }

    /// <summary>
    /// 處理檔案樹節點選取事件
    /// </summary>
    /// <param name="node">被選取節點</param>
    public void HandleTreeSelection(FileTreeNode? node)
    {
        SelectedTreeNode = node;
        if (node is null || !node.IsFile)
        {
            return;
        }

        SelectedFilePath = node.FullPath;
        LoadSelectedFile();
    }

    /// <summary>
    /// 處理 XML 節點選取事件
    /// </summary>
    /// <param name="node">被選取節點</param>
    public void HandleXmlNodeSelection(XmlElementNode? node)
    {
        SelectedXmlNode = node;
        SelectedXmlComment = null;
        SelectedXmlAttribute = null;
    }

    /// <summary>
    /// 處理 XML 註解節點選取事件
    /// </summary>
    /// <param name="comment">被選取註解</param>
    public void HandleXmlCommentSelection(XmlCommentNode? comment)
    {
        SelectedXmlNode = null;
        SelectedXmlComment = comment;
        SelectedXmlAttribute = null;
    }

    private void PickRootDirectory()
    {
        using var dialog = new Forms.FolderBrowserDialog();
        dialog.Description = "選擇掃描根目錄";

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        RootDirectory = dialog.SelectedPath;
    }

    private async Task ScanFilesAsync()
    {
        if (!IsWhitelistEnabled && string.IsNullOrWhiteSpace(RootDirectory))
        {
            EditorStatus = "請先選擇根目錄，或啟用白名單模式";
            return;
        }

        IsScanning = true;
        EditorStatus = "掃描中，請稍候...";

        try
        {
            var whitelistSnapshot = WhitelistItems.ToList();
            var files = await Task.Run(() =>
                _configService.ScanFolderConfig(RootDirectory, IsWhitelistEnabled, whitelistSnapshot));

            _allScannedFiles = files.ToList();
            ApplyFileTreeFilter(expandAll: false);
            SyncBatchSelectableFiles(_allScannedFiles);

            XmlRootNodes.Clear();
            XmlSearchResults.Clear();
            SelectedSearchResult = null;
            SelectedXmlNode = null;
            SelectedXmlAttribute = null;
            BatchAllTargets.Clear();
            BatchFilteredTargets.Clear();
            SelectedBatchTarget = null;
            EditorStatus = $"掃描完成，共 {files.Count} 個檔案";
            BatchStatus = "請加入檔案後再分析共同設定";
        }
        catch (Exception ex)
        {
            EditorStatus = $"掃描失敗：{ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ApplyFileTreeFilter(bool expandAll = false)
    {
        if (_allScannedFiles.Count == 0)
        {
            FileTreeNodes.Clear();
            return;
        }

        var keyword = (FileTreeSearchText ?? string.Empty).Trim();
        IReadOnlyList<string> filteredFiles;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            filteredFiles = _allScannedFiles;
        }
        else
        {
            filteredFiles = _allScannedFiles
                .Where(file => Path.GetFileName(file).Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        BuildFileTree(filteredFiles);

        if (expandAll)
        {
            foreach (var node in FileTreeNodes)
            {
                SetExpandedRecursively(node, true);
            }
        }
    }

    private void ClearFileTreeSearch()
    {
        FileTreeSearchText = string.Empty;
        ApplyFileTreeFilter(expandAll: false);
    }

    private void BuildFileTree(IReadOnlyList<string> files)
    {
        FileTreeNodes.Clear();

        foreach (var file in files)
        {
            var displayPath = GetDisplayPath(file);
            var segments = displayPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            var current = FileTreeNodes;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];
                var folderNode = current.FirstOrDefault(node => !node.IsFile && node.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (folderNode is null)
                {
                    folderNode = new FileTreeNode
                    {
                        Name = segment,
                        FullPath = string.Empty,
                        IsFile = false,
                    };

                    current.Add(folderNode);
                }

                current = folderNode.Children;
            }

            current.Add(new FileTreeNode
            {
                Name = segments[^1],
                FullPath = file,
                IsFile = true,
            });
        }
    }

    private string GetDisplayPath(string fullPath)
    {
        if (!IsWhitelistEnabled && !string.IsNullOrWhiteSpace(RootDirectory))
        {
            try
            {
                return Path.GetRelativePath(RootDirectory, fullPath);
            }
            catch
            {
                return fullPath;
            }
        }

        return fullPath;
    }

    private void LoadSelectedFile()
    {
        XmlRootNodes.Clear();
        XmlSearchResults.Clear();
        SelectedSearchResult = null;
        SelectedXmlNode = null;
        SelectedXmlAttribute = null;
        _currentXmlDocument = null;

        if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            EditorStatus = "尚未選取檔案";
            return;
        }

        if (_isPlainTextMode)
        {
            PlainTextContent = File.ReadAllText(SelectedFilePath);
            EditorStatus = $"已載入：{SelectedFilePath}";
            return;
        }

        try
        {
            _currentXmlDocument = _xmlService.LoadXDocument(SelectedFilePath);
            if (_currentXmlDocument.Root is null)
            {
                EditorStatus = "檔案沒有可編輯的 XML Root";
                return;
            }

            var rootNode = XmlElementNode.FromElement(_currentXmlDocument.Root);
            XmlRootNodes.Add(rootNode);
            SelectedXmlNode = rootNode;
            XmlSearchResults.Clear();
            EditorStatus = $"已載入 XML：{SelectedFilePath}";
        }
        catch (Exception ex)
        {
            EditorStatus = $"載入失敗：{ex.Message}";
        }
    }

    private void SaveCurrentFile()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            EditorStatus = "找不到目標檔案，請重新選取";
            return;
        }

        if (IsPlainTextMode)
        {
            try
            {
                File.WriteAllText(SelectedFilePath, PlainTextContent);
                EditorStatus = $"已儲存：{SelectedFilePath}";
            }
            catch (Exception ex)
            {
                EditorStatus = $"儲存失敗：{ex.Message}";
            }
            return;
        }

        if (_currentXmlDocument is null || XmlRootNodes.Count == 0)
        {
            EditorStatus = "目前沒有可儲存的 XML 內容";
            return;
        }

        try
        {
            foreach (var root in XmlRootNodes)
            {
                root.ApplyToElement();
            }

            _xmlService.SaveXDocument(SelectedFilePath, _currentXmlDocument);
            EditorStatus = $"已儲存：{SelectedFilePath}";
        }
        catch (Exception ex)
        {
            EditorStatus = $"儲存失敗：{ex.Message}";
        }
    }

    private void AddWhitelistFile()
    {
        var dialog = new Win32.OpenFileDialog
        {
            Filter = "Config/XML|*.config;*.xml",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var file in dialog.FileNames)
        {
            if (!WhitelistItems.Contains(file, StringComparer.OrdinalIgnoreCase))
            {
                WhitelistItems.Add(file);
            }
        }
    }

    private void AddWhitelistFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog();
        dialog.Description = "新增白名單資料夾";

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        if (!WhitelistItems.Contains(dialog.SelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            WhitelistItems.Add(dialog.SelectedPath);
        }
    }

    private void RemoveWhitelistItem()
    {
        if (string.IsNullOrWhiteSpace(SelectedWhitelistItem))
        {
            return;
        }

        WhitelistItems.Remove(SelectedWhitelistItem);
        SelectedWhitelistItem = null;
    }

    private void AddSelectedFileToBatch()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            return;
        }

        if (BatchSelectedFiles.Any(path => path.Equals(SelectedFilePath, StringComparison.OrdinalIgnoreCase)))
        {
            BatchStatus = "檔案已在批次清單中";
            return;
        }

        BatchSelectedFiles.Add(SelectedFilePath);
        BatchStatus = $"已加入批次清單：{SelectedFilePath}";
        RaiseCommandStates();
    }

    private void AddCheckedFilesToBatch()
    {
        var selectedItems = BatchSelectableFiles.Where(item => item.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            BatchStatus = "請先在可批次檔案清單勾選至少一個檔案";
            return;
        }

        var addedCount = 0;
        foreach (var item in selectedItems)
        {
            if (BatchSelectedFiles.Any(path => path.Equals(item.FilePath, StringComparison.OrdinalIgnoreCase)))
            {
                item.IsSelected = false;
                continue;
            }

            BatchSelectedFiles.Add(item.FilePath);
            item.IsSelected = false;
            addedCount++;
        }

        BatchStatus = addedCount > 0
            ? $"已加入 {addedCount} 個檔案到批次清單"
            : "勾選檔案已都存在於批次清單";
        RaiseCommandStates();
    }

    private void AddCheckedTreeFilesToBatch()
    {
        var checkedFiles = FileTreeNodes
            .SelectMany(EnumerateCheckedFileNodes)
            .Select(node => node.FullPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (checkedFiles.Count == 0)
        {
            BatchStatus = "請先在設定檔樹狀清單勾選至少一個檔案";
            return;
        }

        var addedCount = 0;
        foreach (var file in checkedFiles)
        {
            if (BatchSelectedFiles.Any(path => path.Equals(file, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            BatchSelectedFiles.Add(file);
            addedCount++;
        }

        foreach (var node in FileTreeNodes.SelectMany(EnumerateFileNodes))
        {
            node.IsChecked = false;
        }

        BatchStatus = addedCount > 0
            ? $"已由 TreeView 多選加入 {addedCount} 個檔案"
            : "勾選檔案已都存在於批次清單";
        RaiseCommandStates();
    }

    private void RemoveBatchFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        BatchSelectedFiles.Remove(filePath);
        BatchStatus = $"已移除：{filePath}";
        RaiseCommandStates();
    }

    private void SyncBatchSelectableFiles(IEnumerable<string> files)
    {
        var selectedSet = BatchSelectedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        BatchSelectableFiles.Clear();
        foreach (var file in files.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            BatchSelectableFiles.Add(new SelectableFileItem
            {
                FilePath = file,
                IsSelected = selectedSet.Contains(file),
            });
        }
    }

    private static IEnumerable<FileTreeNode> EnumerateCheckedFileNodes(FileTreeNode node)
    {
        if (node.IsFile && node.IsChecked)
        {
            yield return node;
        }

        foreach (var child in node.Children)
        {
            foreach (var descendant in EnumerateCheckedFileNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<FileTreeNode> EnumerateFileNodes(FileTreeNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in EnumerateFileNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private static void SetExpandedRecursively(FileTreeNode node, bool expanded)
    {
        node.IsExpanded = expanded;
        foreach (var child in node.Children)
        {
            SetExpandedRecursively(child, expanded);
        }
    }

    private void RefreshBatchSettings()
    {
        BatchAllTargets.Clear();
        BatchFilteredTargets.Clear();
        SelectedBatchTarget = null;

        if (BatchSelectedFiles.Count == 0)
        {
            BatchStatus = "請先加入至少一個檔案";
            return;
        }

        var commonTargets = _xmlBatchEditorService.AnalyzeBatchTargets(BatchSelectedFiles)
            .Select(MapToBatchEditTarget)
            .ToList();

        foreach (var target in commonTargets)
        {
            BatchAllTargets.Add(target);
        }

        ApplyBatchTargetFilters();

        if (BatchAllTargets.Count == 0)
        {
            BatchStatus = "找不到共同可編輯目標，請確認檔案結構是否一致";
            return;
        }

        var attributeCount = BatchAllTargets.Count(target => target.Kind == BatchTargetKind.Attribute);
        var textCount = BatchAllTargets.Count - attributeCount;
        BatchStatus = $"已找到 {BatchAllTargets.Count} 個共同目標（屬性 {attributeCount}、節點文字 {textCount}）";
    }

    private static BatchEditTarget MapToBatchEditTarget(BatchTarget contract)
    {
        var kind = contract.Kind == Core.Enums.BatchTargetKind.Attribute
            ? BatchTargetKind.Attribute
            : BatchTargetKind.ElementText;

        return new BatchEditTarget
        {
            TargetKey = contract.TargetKey,
            TargetSelector = contract.TargetSelector,
            ValueSelector = contract.ValueSelector,
            Kind = kind,
            Path = contract.TargetSelector,
            TargetAttributeName = kind == BatchTargetKind.Attribute ? contract.ValueSelector.TrimStart('@') : null,
            SampleValue = contract.SampleValue,
        };
    }

    private static BatchTarget MapToBatchTarget(BatchEditTarget target)
    {
        var kind = target.Kind == BatchTargetKind.Attribute
            ? BatchTargetKind.Attribute
            : BatchTargetKind.ElementText;

        return new BatchTarget(
            target.TargetKey,
            target.TargetSelector,
            target.ValueSelector,
            kind,
            target.SampleValue);
    }

    private void ApplyBatch()
    {
        if (SelectedBatchTarget is null)
        {
            BatchStatus = "請先選擇批次目標";
            return;
        }

        var affectedFiles = 0;
        var affectedEntries = 0;

        foreach (var file in BatchSelectedFiles)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var updated = _xmlBatchEditorService.ApplyBatchTarget(file, MapToBatchTarget(SelectedBatchTarget), BatchNewValue);
            if (updated <= 0)
            {
                continue;
            }

            affectedEntries += updated;
            affectedFiles++;
        }

        BatchStatus = $"批次完成：共更新 {affectedFiles} 個檔案，{affectedEntries} 筆設定";

        if (!string.IsNullOrWhiteSpace(SelectedFilePath)
            && BatchSelectedFiles.Contains(SelectedFilePath, StringComparer.OrdinalIgnoreCase))
        {
            LoadSelectedFile();
        }
    }

    private void ApplyBatchTargetFilters()
    {
        BatchFilteredTargets.Clear();

        var keyword = (BatchTargetSearchText ?? string.Empty).Trim();

        IEnumerable<BatchEditTarget> query = BatchAllTargets;
        if (SelectedBatchTargetType == "屬性")
        {
            query = query.Where(target => target.Kind == BatchTargetKind.Attribute);
        }
        else if (SelectedBatchTargetType == "節點文字")
        {
            query = query.Where(target => target.Kind == BatchTargetKind.ElementText);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(target => BuildBatchTargetSearchSource(target).Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var target in query)
        {
            BatchFilteredTargets.Add(target);
        }

        SelectedBatchTarget = BatchFilteredTargets.FirstOrDefault();
    }

    private static string BuildBatchTargetSearchSource(BatchEditTarget target)
    {
        return string.Join(
            " ",
            target.DisplayType,
            target.TargetSelector,
            target.ValueSelector,
            target.SampleValue);
    }

    private void AddChildNode()
    {
        if (SelectedXmlNode is null)
        {
            EditorStatus = "請先選擇要新增子節點的父節點";
            return;
        }

        var newNode = SelectedXmlNode.AddChild(NewChildElementName, NewChildTextValue);
        SelectedXmlNode = newNode;
        EditorStatus = "已新增子節點";
    }

    private void DeleteSelectedNode()
    {
        if (SelectedXmlNode is null)
        {
            EditorStatus = "請先選擇要刪除的節點";
            return;
        }

        if (SelectedXmlNode.IsRoot)
        {
            EditorStatus = "Root 節點不可刪除";
            return;
        }

        var parent = SelectedXmlNode.Parent as XmlElementNode;
        SelectedXmlNode.RemoveFromParent();
        SelectedXmlNode = parent;
        EditorStatus = "已刪除節點";
    }

    private void AddAttribute()
    {
        if (SelectedXmlNode is null)
        {
            EditorStatus = "請先選擇節點";
            return;
        }

        var attributeName = NewAttributeName.Trim();
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            EditorStatus = "屬性名稱不可為空白";
            return;
        }

        var existing = SelectedXmlNode.Attributes.FirstOrDefault(attribute => attribute.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Value = NewAttributeValue;
            EditorStatus = "屬性已存在，已更新其值";
            return;
        }

        SelectedXmlNode.Attributes.Add(new XmlAttributeItem
        {
            Name = attributeName,
            Value = NewAttributeValue,
        });

        NewAttributeName = string.Empty;
        NewAttributeValue = string.Empty;
        EditorStatus = "已新增屬性";
        RaiseCommandStates();
    }

    private void RemoveAttribute()
    {
        if (SelectedXmlNode is null || SelectedXmlAttribute is null)
        {
            EditorStatus = "請先選擇要刪除的屬性";
            return;
        }

        SelectedXmlNode.Attributes.Remove(SelectedXmlAttribute);
        SelectedXmlAttribute = null;
        EditorStatus = "已刪除屬性";
        RaiseCommandStates();
    }

    private void SearchXmlNodes()
    {
        XmlSearchResults.Clear();
        SelectedSearchResult = null;

        if (IsPlainTextMode)
        {
            SearchInPlainText();
            return;
        }

        if (XmlRootNodes.Count == 0)
        {
            EditorStatus = "請先載入 XML 檔案";
            return;
        }

        var query = (XmlSearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            EditorStatus = "請輸入搜尋關鍵字";
            return;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        foreach (var root in XmlRootNodes)
        {
            foreach (var node in root.Traverse())
            {
                if (!IsNodeMatched(node, query, comparison))
                {
                    continue;
                }

                XmlSearchResults.Add(new XmlSearchResult
                {
                    Node = node,
                    ElementName = node.ElementName,
                    Path = BuildNodePath(node),
                    Summary = BuildNodeSummary(node),
                });
            }
        }

        if (XmlSearchResults.Count > 0)
        {
            SelectedSearchResult = XmlSearchResults[0];
            EditorStatus = $"搜尋完成，共找到 {XmlSearchResults.Count} 筆符合項目";
            return;
        }

        EditorStatus = "找不到符合的節點";
    }

    private void SearchInPlainText()
    {
        var query = (XmlSearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            EditorStatus = "請輸入搜尋關鍵字";
            return;
        }

        var text = PlainTextContent;
        var comparison = StringComparison.OrdinalIgnoreCase;
        int offset = 0;
        int matchCount = 0;

        while ((offset = text.IndexOf(query, offset, comparison)) >= 0)
        {
            var lineNumber = text[..offset].Count(c => c == '\n') + 1;
            var lineStart = offset > 0 ? text.LastIndexOf('\n', offset - 1) + 1 : 0;
            var lineEnd = text.IndexOf('\n', offset);
            lineEnd = lineEnd < 0 ? text.Length : lineEnd;
            var lineContent = text[lineStart..lineEnd].Trim();

            XmlSearchResults.Add(new XmlSearchResult
            {
                TextOffset = offset,
                ElementName = $"第 {lineNumber} 行",
                Path = string.Empty,
                Summary = lineContent,
            });

            offset += query.Length;
            matchCount++;
            if (matchCount >= 100) break;
        }

        if (matchCount > 0)
        {
            SelectedSearchResult = XmlSearchResults[0];
            EditorStatus = $"找到 {matchCount} 筆符合項目";
            return;
        }

        EditorStatus = "找不到符合的內容";
    }

    private void ExpandAllXmlNodes()
    {
        foreach (var root in XmlRootNodes)
        {
            root.SetExpandedRecursively(true);
        }

        EditorStatus = "已全部展開";
    }

    private void CollapseAllXmlNodes()
    {
        foreach (var root in XmlRootNodes)
        {
            root.SetExpandedRecursively(false);
            root.IsExpanded = true;
        }

        EditorStatus = "已收合到第一層";
    }

    private void FocusNode(XmlElementNode node)
    {
        foreach (var root in XmlRootNodes)
        {
            foreach (var current in root.Traverse())
            {
                current.IsSelected = false;
            }
        }

        node.ExpandAncestors();
        node.IsExpanded = true;
        node.IsSelected = true;
        SelectedXmlNode = node;
    }

    private static bool IsNodeMatched(XmlElementNode node, string query, StringComparison comparison)
    {
        if (node.ElementName.Contains(query, comparison))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(node.TextValue) && node.TextValue.Contains(query, comparison))
        {
            return true;
        }

        foreach (var attribute in node.Attributes)
        {
            if (attribute.Name.Contains(query, comparison)
                || (!string.IsNullOrWhiteSpace(attribute.Value) && attribute.Value.Contains(query, comparison)))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildNodePath(XmlElementNode node)
    {
        var segments = new Stack<string>();
        var current = node;
        while (current is not null)
        {
            segments.Push(current.ElementName);
            current = current.Parent as XmlElementNode;
        }

        return "/" + string.Join("/", segments);
    }

    private static string BuildNodeSummary(XmlElementNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.TextValue))
        {
            return node.TextValue;
        }

        if (node.Attributes.Count > 0)
        {
            var firstAttribute = node.Attributes[0];
            return $"{firstAttribute.Name}={firstAttribute.Value}";
        }

        return "(無文字內容)";
    }

    /// <summary>
    /// 刪除選取的註解節點
    /// </summary>
    private void DeleteSelectedComment()
    {
        if (SelectedXmlComment is null)
        {
            return;
        }

        var parent = SelectedXmlComment.Parent as XmlElementNode;
        SelectedXmlComment.RemoveFromParent();
        SelectedXmlComment = null;
        SelectedXmlNode = parent;
        EditorStatus = "已刪除註解";
    }

    /// <summary>
    /// 將選取的註解轉為可編輯元素（將註解內容視為元素文字）
    /// </summary>
    private void UncommentSelectedComment()
    {
        if (SelectedXmlComment is null)
        {
            return;
        }

        if (SelectedXmlComment.Parent is not XmlElementNode parent)
        {
            return;
        }

        var commentText = SelectedXmlComment.CommentText.Trim();

        // 若註解內容本身是合法的 XML，直接還原為元素而非衍生元素名稱
        XElement? parsed = null;
        if (commentText.StartsWith("<") && !commentText.StartsWith("<!--"))
        {
            try { parsed = XElement.Parse(commentText); } catch { }
        }

        SelectedXmlComment.RemoveFromParent();

        XmlElementNode newElement;
        if (parsed != null)
        {
            newElement = parent.AddChildElement(parsed);
            EditorStatus = $"已將 XML 註解還原為元素 <{parsed.Name.LocalName} />";
        }
        else
        {
            var elementName = XmlCommentNode.ConvertCommentToElementName(commentText);
            newElement = parent.AddChild(elementName, commentText);
            EditorStatus = $"已將註解轉為元素 <{elementName} />";
        }

        SelectedXmlComment = null;
        SelectedXmlNode = newElement;
    }

    private void RaiseCommandStates()
    {
        (ScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveWhitelistItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (SaveCurrentFileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddSelectedFileToBatchCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ApplyBatchCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddChildNodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteSelectedNodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AddAttributeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveAttributeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DeleteCommentCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (UncommentCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

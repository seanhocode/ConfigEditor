namespace ConfigEditor.Core.Services{
    /// <summary>
    /// 掃描設定檔清單的用例介面
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 掃描根目錄或白名單項目，回傳可處理的設定檔路徑
        /// </summary>
        /// <param name="rootDirectory">預設掃描根目錄</param>
        /// <param name="whitelistEnabled">是否啟用白名單模式</param>
        /// <param name="whitelistItems">白名單檔案或資料夾清單</param>
        /// <returns>排序後的檔案絕對路徑清單</returns>
        IReadOnlyList<string> ScanFolderConfig(string rootDirectory, bool whitelistEnabled, IEnumerable<string> whitelistItems);
    }
}
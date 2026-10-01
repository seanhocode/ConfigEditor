# ConfigEditor

ConfigEditor 的 `SeanTool.CSharp.XmlTool`、`SeanTool.CSharp.WPFTool` 依賴來自本儲存庫 `.nuget\local` 中的版本化 nupkg（來源定義於 `NuGet.Config`），不需要跨資料夾的專案參照。更新 ToolKit 時，在 `SeanTool.CSharp` 分別 pack `Src\XmlTool\XmlTool.csproj` 與 `Src\WPFTool\WPFTool.csproj`，將新版 nupkg 放到 `.nuget\local` 並同步更新相應 `.csproj` 的 `PackageReference` 版本；建置前執行 `dotnet restore ConfigEditor.slnx`。

WPF 樣式由 `WPFTool;component/Styles/Theme.xaml` 載入；`ConfigEditor.Wpf\Themes\Layout.xaml` 只保留本應用的精簡 UI 背景切換。XML 編輯樹仍使用原生 WPF TreeView，以保留可編輯節點、註解及搜尋定位。

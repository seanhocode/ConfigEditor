namespace ConfigEditor.Infrastructure.Tests;

/// <summary>
/// 測試用的 XML 和 Config 檔案內容資源庫
/// </summary>
public static class TestResources
{
    public static string CustomizeModule1Xml => @"<?xml version=""1.0"" encoding=""utf-8""?>
<!--Tag: 要被取代的標籤-->
<!--Value: 要取代的值-->
<ReplaceCfgMap>
  <property Tag=""@INSTALL_PATH@"" Value=""C:\WebRoot\eServices\"" />
  <property Tag=""@WEB_ROOT_URL@"" Value=""http://app.example.com/"" />
  <property Tag=""@LOG_PATH@"" Value=""C:\LogFiles\"" />
  <property Tag=""@HANDLE_PATH@"" Value=""C:\WebRoot\Handler\"" />
  <property Tag=""@DB_NAME@"" Value=""Server1"" />
  <property Tag=""@DB_USR_ID@"" Value=""User01"" />
  <property Tag=""@DB_USR_PWD@"" Value=""test-password-01"" />
  <property Tag=""@1_DB@"" Value=""DB01"" />
  <property Tag=""@2_DB@"" Value=""DB02"" />
  <property Tag=""@3_DB@"" Value=""DB03"" />
  <property Tag=""@4_DB@"" Value=""DB04"" />
  <property Tag=""@5_DB@"" Value=""DB05"" />
  <property Tag=""@LOG_DB@"" Value=""DB06"" />
  <property Tag=""@ENV@"" Value="""" />
  <property Tag=""@WorkerCount@"" Value=""3"" />
  <property Tag=""@RetryCount@"" Value=""5"" />
  <property Tag=""@EnableAccess@"" Value=""true"" />
  <property Tag=""@IS_USE_SSO@"" Value=""N"" />
  <property Tag=""@SSO_API_BaseUrl@"" Value=""http://app.example.com/api/v2"" />
  <property Tag=""@SSO_API_TokenUrl@"" Value=""sso/Token"" />
  <property Tag=""@PortalUrl@"" Value=""http://app.example.com/personal/"" />
  <property Tag=""@IS_USE_UI_SECURITY@"" Value=""N"" />
  <property Tag=""@SHOW_WATER_MARK_URL@"" Value=""http://app.example.com/GetWaterMark"" />
  <property Tag=""@HOST_NAME@"" Value=""AP01"" />
  <property Tag=""@ConnStr1@"" Value=""test-connstr-01"" />
  <property Tag=""@ConnStr2@"" Value=""test-connstr-02"" />
</ReplaceCfgMap>";

    public static string CustomizeModule2Xml => @"<?xml version=""1.0"" encoding=""utf-8""?>
<!--Tag: 要被取代的標籤-->
<!--Value: 要取代的值-->
<ReplaceCfgMap>
    <property Tag=""@INSTALL_PATH@"" Value=""C:\WebRoot\eServices\"" />
    <property Tag=""@WEB_ROOT_URL@"" Value=""http://app.example.com/"" />
    <property Tag=""@LOG_PATH@"" Value=""C:\LogFiles\"" />
    <property Tag=""@HANDLE_PATH@"" Value=""C:\WebRoot\Handler\"" />
    <property Tag=""@DB_NAME@"" Value=""Server1"" />
    <property Tag=""@DB_USR_ID@"" Value=""User01"" />
    <property Tag=""@DB_USR_PWD@"" Value=""test-password-01"" />
    <property Tag=""@1_DB@"" Value=""DB01"" />
    <property Tag=""@2_DB@"" Value=""DB02"" />
    <property Tag=""@3_DB@"" Value=""DB03"" />
    <property Tag=""@4_DB@"" Value=""DB04"" />
    <property Tag=""@5_DB@"" Value=""DB05"" />
    <property Tag=""@6_DB@"" Value=""DB06"" />
    <property Tag=""@ENV@"" Value="""" />
    <property Tag=""@WorkerCount@"" Value=""5"" />
    <property Tag=""@RetryCount@"" Value=""5"" />
    <property Tag=""@EnableAccess@"" Value=""true"" />
    <property Tag=""@IS_USE_SSO@"" Value=""N"" />
    <property Tag=""@IS_USE_SSO_COOKIE_CHK@"" Value=""N"" />
    <property Tag=""@SSO_API_BaseUrl@"" Value=""http://app.example.com/api/v2"" />
    <property Tag=""@SSO_API_CheckTokenUrl@"" Value=""sso/CheckToken"" />
    <property Tag=""@SSO_API_TokenUrl@"" Value=""sso/Token"" />
    <property Tag=""@SSO_API_LogoutUrl@"" Value=""sso/Logout"" />
    <property Tag=""@SSO_API_FetchPerSerilNoUrl@"" Value=""User/FetchPerSerilNo"" />
    <property Tag=""@SSO_API_SSOInfoBaseUrl@"" Value=""http://app.example.com/"" />
    <property Tag=""@SSO_API_SSOInfoUrl@"" Value=""sso/SsoInfo"" />
    <property Tag=""@PortalUrl@"" Value=""http://app.example.com/personal/"" />
    <property Tag=""@IS_USE_UI_SECURITY@"" Value=""N"" />
    <property Tag=""@SHOW_WATER_MARK_URL@"" Value=""http://app.example.com/GetWaterMark"" />
    <property Tag=""@HOST_NAME@"" Value=""AP01"" />    
    <property Tag=""@ConnStr1@"" Value=""test-connstr-01"" />
    <property Tag=""@ConnStr2@"" Value=""test-connstr-02"" />
</ReplaceCfgMap>";

    public static string CustomizeModule1Config => @"<?xml version=""1.0"" encoding=""utf-8""?>
<customize xmlns=""http://www.springframework.net"" xmlns:db=""http://www.springframework.net/database"">
    <config name=""Objects.config"">
        <replace id=""HttpAsyncTaskServer"" target=""*"">
            <constructor-arg name=""collection"" type=""System.Collections.Generic.IEnumerable&lt;String&gt;"">
                <list element-type=""string"">
                    <value>http://app.example.com/Service</value>
                </list>
            </constructor-arg>
        </replace>
    </config>
    <config name=""Config.config"">
        <replace id=""InfrastructureConfig"" target=""s:property[@name='IntegrateWithEnversEnabled']"">
            <property name=""IntegrateWithEnversEnabled"" value=""true"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='OnlineHelpURL']"">
            <property name=""OnlineHelpURL"" value="""" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='WebTitle']"">
            <property name=""WebTitle"" value=""Module1"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='Module3RootUrl']"">
            <property name=""Module3RootUrl"" value=""http://app.example.com/Module3"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='ServerRootUrl']"">
            <property name=""ServerRootUrl"" value=""http://app.example.com/Server"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='ServerClientID']"">
            <property name=""ServerClientID"" value=""test-client-id-01"" />
        </replace>
    </config>
</customize>";

    public static string CustomizeModule2Config => @"<?xml version=""1.0"" encoding=""utf-8""?>
<customize xmlns=""http://www.springframework.net"" xmlns:db=""http://www.springframework.net/database"">
    <config name=""Objects.config"">
        <replace id=""HttpAsyncTaskServer"" target=""*"">
            <constructor-arg name=""collection"" type=""System.Collections.Generic.IEnumerable&lt;String&gt;"">
                <list element-type=""string"">
                    <value>http://app.example.com/MainWeb</value>
                </list>
            </constructor-arg>
        </replace>
    </config>
    <config name=""Config.config"">
        <replace id=""InfrastructureConfig"" target=""s:property[@name='IntegrateWithEnversEnabled']"">
            <property name=""IntegrateWithEnversEnabled"" value=""true"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='OnlineHelpURL']"">
            <property name=""OnlineHelpURL"" value="""" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='WebTitle']"">
            <property name=""WebTitle"" value=""Module2"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='Module3RootUrl']"">
            <property name=""Module3RootUrl"" value=""http://app.example.com/Module3"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='ServerRootUrl']"">
            <property name=""ServerRootUrl"" value=""http://app.example.com/Server"" />
        </replace>
        <replace id=""UIConfig"" target=""s:property[@name='ServerClientID']"">
            <property name=""ServerClientID"" value=""test-client-id-01"" />
        </replace>
    </config>
</customize>";

    /// <summary>
    /// 簡單的 XML 測試檔案（元素文字類型）
    /// </summary>
    public static string SimpleElementTextXml => @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <setting>
    <value>original-value</value>
  </setting>
</root>";

    /// <summary>
    /// 生成指定數量的虛擬 Config 檔案內容
    /// </summary>
    public static string GenerateDummyConfig(int count) => @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>" + string.Concat(Enumerable.Range(1, count).Select(i => $@"
  <item name=""item{i}"" value=""value{i}"" />")) + @"
</root>";
}

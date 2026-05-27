using System.Reflection;

namespace WebFunControl.Models;

/// <summary>
/// 应用版本信息 — 版本号由GitHub Actions工作流注入
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// 获取应用版本号（从程序集InformationalVersion读取，CI中通过/property:Version=覆盖）
    /// </summary>
    public static string Current =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0-dev";

    public static string AppName => "WebFunControl";
    public static string Description => "W66D 风扇智能控制面板";
    public static string ProjectUrl => "https://github.com/wwiinnddyy/WebFunControl";
}

using Microsoft.AspNetCore.Components.WebView.Maui;
using Rune.Desktop;
using Rune.UI;

namespace LearnSite;

/// <summary>桌面应用的启动：一个窗口，整页是 BlazorWebView，根组件是 <see cref="Components.Routes"/>。</summary>
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        // UseRuneDesktop 去掉系统标题栏，AppShell 的顶栏就是窗口的标题栏（可拖动、双击最大化）
        builder.UseMauiApp<App>().UseRuneDesktop();

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        // F12 打开 WebView2 的开发者工具
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
        return builder.Build();
    }
}

/// <summary>应用：一个窗口。</summary>
public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var webView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html",
            RootComponents = { new RootComponent { Selector = "#app", ComponentType = typeof(Components.Routes) } },
        };

        // 页面画出来之前窗口的底色：同 Rune 内置亮、暗主题的 Background，跟随系统明暗，避免启动时闪一下
        var page = new ContentPage { Content = webView };
        page.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#f9f9fb"), Color.FromArgb("#111113"));
        return new Window(page) { Title = "C# 学习站" };
    }
}

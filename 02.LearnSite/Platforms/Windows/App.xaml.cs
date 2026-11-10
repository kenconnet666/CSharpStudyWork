namespace LearnSite.WinUI;

/// <summary>WinUI 的应用入口：交给 MAUI。</summary>
public partial class App : MauiWinUIApplication
{
    public App() => InitializeComponent();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

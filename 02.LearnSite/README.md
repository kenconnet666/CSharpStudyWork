# 02.LearnSite：C# 互动学习站

把 C# 里"看不见"的东西摆出来：每一课是一个页面，左边讲解，右边是可以点、可以拖的演示。
形式是 **.NET MAUI 桌面窗口 + Blazor WebView**（Blazor Hybrid），界面用 [RuneForBlazor](https://www.nuget.org/packages/RuneForBlazor.UI) 组件库。
组件在本进程里运行，WebView2 只负责显示，所以以后 Socket、串口的演示可以直接用真实的网络和串口。

## 已有的课

| 页面 | 内容 |
|---|---|
| 位与字节 | 点 8 个位格；`\| & ^ ~ << >>` 逐位对齐着看；用一个字节存几个开关（Flags） |
| 大小端 | 输入一个数，看大端、小端两种排法，以及"字节序读错"读成什么（16 位、32 位） |
| 值类型与引用类型 | struct 与 class 并排：赋值、修改后，变量与对象怎么变 |

## 运行

1. 一次性准备：安装 MAUI 的 Windows 工作负载（需要管理员权限，下载约几百 MB）：
   ```bash
   dotnet workload install maui-windows
   ```
2. 在 Rider 里：运行配置选 `LearnSite`（Windows Machine），点绿色三角。
   命令行：`dotnet run --project 02.LearnSite`
3. 调试版按 **F12** 可以打开 WebView2 的开发者工具，看页面结构和样式。

## 新增一课

1. 在 `Components/Pages/` 里新建一个带 `@page "/路径"` 的 `.razor` 页面。
2. 在 `Components/LessonNav.razor` 里加一项导航。
3. 演示里反复用到的小部件放在 `Components/Visuals/`（现有：`BitRow` 位格、`ByteCells` 字节格、`Slot` 变量/对象盒子、`Readout` 读数）。

页面里显示的数字都是 C# 当场计算出来的，不是写死的动画。

## 关于组件库

默认使用 nuget.org 上已发布的 `RuneForBlazor.*`（0.17.0）。如果需要改组件库：在 `RuneForBlazor` 仓库改好后，
用它自带的 `tools/pack/pack.sh <输出目录> 0.18.0-local.N` 打本地包，在本仓库加一个指向该目录的 `nuget.config`，
再把 `PackageReference` 的版本改成同一个号。每次重新打包要**递增版本号**，否则 NuGet 会继续用缓存里的旧包。

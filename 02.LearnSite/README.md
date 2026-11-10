# 02.LearnSite：C# 互动学习站

把 C# 里"看不见"的东西摆出来：每一课是一个页面，左边讲解，右边是可以点、可以拖的演示。
形式是 **.NET MAUI 桌面窗口 + Blazor WebView**（Blazor Hybrid），界面用 [RuneForBlazor](https://www.nuget.org/packages/RuneForBlazor.UI) 组件库。
组件在本进程里运行，WebView2 只负责显示，所以以后 Socket、串口的演示可以直接用真实的网络和串口。

## 已有的课

| 页面 | 内容 |
|---|---|
| 位与字节 | 拨 8 个开关，"权重相加"变成算式；位运算做成竖式，每列写出怎么算的，移位会告诉你掉出去了什么；设备面板上 4 盏报警灯对应状态字节的 4 位（Flags） |
| 大小端 | 同一个字节在两种排法里颜色相同、位置相反；"发送方 → 线路 → 接收方"动画，字节飞过去，双方字节序一致/不一致分别读出什么 |
| 值类型与引用类型 | struct 是复印件、class 是同一份在线文档：并排演示，class 一侧画出箭头，改一个另一个跟着变 |

## 让它"看得见、记得住"的做法

- **代码高亮**：代码块用 Rune 的 `CodeBlock`（内置 Shiki，随包离线可用），代码随演示实时变化、实时上色。
- **解说气泡**（`Narration`）：每次操作后用大白话解释"刚才发生了什么、为什么"。
- **先猜再看**（`Quiz`）：每个演示配几道小测验，选完给对错和原因，可以重答。
- **动画**：位翻转、新元素出现、数值变化都有轻微的弹入/闪动，方便看出"哪里变了"。

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

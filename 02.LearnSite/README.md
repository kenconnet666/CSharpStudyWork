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
| 参数传递：ref / out | 单步回放（默认、ref、out 三种方式，高亮当前执行的那一行，左右两个盒子看值怎么走）；交换两个数，不写 ref 交换不了；用 int.TryParse 看 out 的真实用法 |

## 语法精讲（对应 01.CSharpSyntax 的第 1~11 课）

| 页面 | 演示 |
|---|---|
| 基础：类型、格式化、空值 | 类型与 `var` 推断、整数除法的坑；格式说明符（F2/X2/P1…）实时看结果；`?.` `??` `is null` 在 null / 空串 / 有值下的真实结果；范围索引与元组 |
| 类与对象 | 公开字段 vs 属性（带校验，拖到负数看区别）；record 与 class 的相等、`with`；`init` / `required` 的编译检查模拟 |
| 继承、多态、接口 | 同一变量类型装三种设备，调用同一方法；`override` 与 `new` 的 2×2 对照；依赖接口换协议不用改调用方 |
| 泛型与集合 | List / Dictionary / HashSet / Queue 操场（真实集合，点按钮看内容变化和异常）；环形缓冲区；`yield` 单步执行 |
| LINQ | 数据流水线开关；分组统计；惰性执行（谓词被调用几次，亮起被检查的订单） |
| 模式匹配 | 列表模式解析 Modbus 帧、关系模式、类型/属性模式、元组模式状态机——匹配到的那条分支在代码里高亮 |
| async / await | 三个任务的**真实时间线**（依次、WhenAll、WhenAny、超时、取消）；UI 线程在三种写法里在干什么 |
| 异常、资源、线程安全 | try/catch/finally 与 using 释放顺序的单步执行；真的开多线程抢计数器（不保护 vs lock vs Interlocked） |
| C# 12~14 新特性 | `field` 关键字、扩展成员、`?.=`、集合表达式与展开 |

## 宽屏布局

每个演示是左右两栏：**左边演示（可以点、可以拖），右边代码 + 解说 + 小测验**，操作和代码永远在同一屏；窗口变窄时自动上下排。
页面最宽约 1760 像素并居中，不再窄窄一条。公共部件：`LessonPage`（页面外框）、`Demo`（两栏卡片）、`TraceDemo`（单步执行：左代码、右变量与控制台输出）。

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

1. 在 `Components/Pages/` 对应的分组里新建一个带 `@page "/路径"` 的 `.razor` 页面：`Foundations/`（看见底层）、`Syntax/`（语法精讲）、`Communication/`（通信）。
2. 在 `Components/Layout/LessonNav.razor` 里加一项导航。
3. 通用部件放在 `Components/Visuals/`：`Teaching/`（`LessonPage`、`Demo`、`TraceDemo` 逐行执行、`ConceptGuide`/`Concept` 概念讲解、`Narration`、`Quiz`）、`Data/`（`Slot` 变量盒子、`Readout` 读数、`BitRow`、`ByteCells`、`FrameView` 报文字段）、`Animation/`（`ParamFlow`、`EventStage`、`WireDemo`、`RefDiagram`）。
4. 真实的本机 TCP 和 Modbus 模拟设备在 `Services/`（`LoopbackServer` 是公共基类）。

页面里显示的数字都是 C# 当场计算出来的，不是写死的动画。

## 数据库课程（「数据库」与「综合实战」）

这几课连的是 WSL 里 Docker 中的 PostgreSQL（EasyQuerySharp 项目自带的开发库），真实读写，不是模拟：

1. 启动数据库：在 EasyQuerySharp 目录里执行 `bash infra/dev/db.sh up -d`（端口 45432）。
2. 建一个学习站专用的库和账号（只需一次；密码自己定，不要提交）：
   `wsl -d Debian --exec docker exec easy-query-sharp-dev-postgres-1 psql -U postgres -c "CREATE ROLE learnsite LOGIN PASSWORD '你的密码'" -c "CREATE DATABASE learnsite OWNER learnsite"`
3. 把连接串放进用户目录的 `.learnsite\connection.txt`（或环境变量 `LEARNSITE_PG`）：
   `Host=127.0.0.1;Port=45432;Username=learnsite;Password=你的密码;Database=learnsite`

表和示例设备在第一次进入这些页面时自动创建（EasyQuerySharp 的 CodeFirst）。库没启动时页面会显示原因和启动命令。
EasyQuerySharp 目前是**本地项目引用**（`..\..\EasyQuerySharp`），验证完后改回 nuget.org 的正式版本。

## 关于组件库

默认使用 nuget.org 上已发布的 `RuneForBlazor.*`（0.17.0）。如果需要改组件库：在 `RuneForBlazor` 仓库改好后，
用它自带的 `tools/pack/pack.sh <输出目录> 0.18.0-local.N` 打本地包，在本仓库加一个指向该目录的 `nuget.config`，
再把 `PackageReference` 的版本改成同一个号。每次重新打包要**递增版本号**，否则 NuGet 会继续用缓存里的旧包。

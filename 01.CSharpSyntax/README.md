# 01.CSharpSyntax：C# 语法课程（.NET 10 / C# 14）

面向有 Java、JS 基础的同学，11 课，每课一个文件，代码里的中文注释就是讲义。

## 怎么运行

### 在 Rider 里（推荐）
1. 打开解决方案 `StudyWork.slnx`，解决方案视图里能看到 `CSharpSyntax` 项目。
2. 右键 `CSharpSyntax` → **运行**；或点右上角 **添加配置 → .NET 项目 → CSharpSyntax**，之后用绿色三角运行。
3. 底部"运行"窗口出现菜单，在窗口里输入课程编号回车即可（`a` 运行全部，`q` 退出）。
4. 想直接运行某一课：运行配置 → **程序参数** 填 `3`（第 3 课）或 `all`。
5. 调试：在某一行左侧点一下加断点，用甲虫图标调试运行，F8 单步，观察变量。这是理解语法最快的方式。

### 命令行
```bash
cd 01.CSharpSyntax
dotnet run              # 菜单
dotnet run -- 3         # 只运行第 3 课（注意 -- 后面是程序参数）
dotnet run -- all       # 全部
```

## 怎么学（每课约 20~40 分钟）

1. **先读后跑**：打开 `Lessons/LessonXX.cs`，从上往下读注释和代码，先猜输出。
2. **运行对照**：运行该课，核对输出和你的猜测，不一致的地方就是要重点学的点。
3. **动手改**：改一个值、删一个 `override`、去掉一个 `lock`，看编译错误或结果变化。编译错误是最好的老师。
4. **跳转看定义**：光标放在 `List`、`Task`、`Func` 上按 `Ctrl+B`，看标准库源码和文档。
5. **写练习**：每课末尾的练习自己写在新文件里，不要看答案。

## 课程目录

| 课 | 文件 | 重点 | 与 Java/JS 的最大差异 |
|---|---|---|---|
| 1 | Lesson01Basics | 变量、字符串插值、`?.` `??`、元组、范围索引 | `var` 是静态推断；可空引用类型 |
| 2 | Lesson02Types | 值类型/引用类型、record、enum Flags、byte 与字节序 | `byte` 无符号；struct；`ref/out` |
| 3 | Lesson03Classes | 属性、主构造函数、`init`/`required`、索引器 | **属性**取代 getter/setter |
| 4 | Lesson04Oop | 继承、接口、`virtual/override`、`sealed` | 方法默认不可重写 |
| 5 | Lesson05GenericsCollections | `List`/`Dictionary`、集合表达式、泛型约束、`yield` | 真泛型，不擦除 |
| 6 | Lesson06DelegatesEvents | 委托、`Action/Func`、Lambda、`event` | 语言级事件机制，上位机核心 |
| 7 | Lesson07Linq | `Where/Select/GroupBy`、惰性求值 | 类似 Stream，但更丰富 |
| 8 | Lesson08PatternMatching | switch 表达式、属性/列表模式 | 解析协议帧神器 |
| 9 | Lesson09Async | `async/await`、`WhenAll`、取消、UI 线程规则 | 有真线程，UI 线程要小心 |
| 10 | Lesson10ExceptionsResources | `using`、`IDisposable`、`lock` | 串口/Socket 必须释放 |
| 11 | Lesson11NewFeatures | `field`、扩展成员、`?.=`（C# 14） | 本课全是新特性 |

## 每课练习（自己写）

1. 写一个方法 `(double Avg, double Max) Stats(IEnumerable<double>)`，用元组返回，并用 `TryParse` 读取用户输入的一串数字。
2. 定义 `record Point3D(double X, double Y, double Z)`，写出"两点距离"的 `static` 方法；比较两个相同值的 record 与两个相同值的 class。
3. 写 `class Counter`，属性 `Count` 外部只读；用 `field` 关键字让 `Step` 属性不接受小于 1 的值。
4. 定义 `IShape`，写 `Circle` / `Rect` 两个实现，放进 `List<IShape>`，用多态求总面积。
5. 写一个泛型 `class Cache<TKey, TValue>`，用 `Dictionary` 实现，`TryGet` 不存在时返回 `false`。
6. 写一个 `Timer` 类，每次调用 `Tick()` 触发 `event Action<int> Ticked`；用 `+=` 订阅，再 `-=` 退订并验证。
7. 给 `List<Reading>` 写 LINQ：按传感器分组，求每组平均值并按平均值降序输出。
8. 用 switch 表达式 + 列表模式解析 `byte[]`：`[0x01, var len, ..var data]`，判断 `data.Length == len`。
9. 写 `async Task<int> RetryAsync(Func<Task<int>> action, int times)`，失败重试，用 `CancellationToken` 支持取消。
10. 写一个类实现 `IDisposable`，在 `Dispose` 中打印日志，分别用 `using` 块和 `using` 声明测试释放顺序。

## 强化练习与扩展（第 12~17 项，带完整答案）

`Exercises/` 目录下有 6 个文件，每道题的注释都按固定格式写：**题目 → 思路 → 答案代码 → 预期输出**。
建议先只看【题目】，自己在纸上或新文件里写，再对照答案；预期输出已经全部运行核对过（只有"不保护的计数"这一项每次运行不同）。

| 编号 | 文件 | 覆盖内容 | 对应课 |
|---|---|---|---|
| 12 | Exercise01BasicsTypes | 元组 + TryParse、record 相等性、Flags 位操作、大小端、byte 陷阱、进制转换 | 1、2 |
| 13 | Exercise02Classes | `field` 属性、接口多态、索引器校验、`override` 与 `new`、组合优于继承 | 3、4 |
| 14 | Exercise03CollectionsEvents | 有序环形缓冲区、泛型缓存、yield 惰性、事件退订、闭包陷阱、重试方法 | 5、6 |
| 15 | Exercise04LinqPatterns | 分组求和、MaxBy、Chunk、延迟执行陷阱、列表模式解析 Modbus 帧、状态机 | 7、8 |
| 16 | Exercise05AsyncResources | 带取消的重试、`WaitAsync` 超时、`await using`、`Interlocked`、并发字典 | 9、10 |
| 17 | Exercise06ProtocolExtension | CRC16、Modbus RTU/TCP 请求帧构造与解析、float 字序、`Span`、`Channel` | 全部 |

运行方式和课程一样：`dotnet run -- 12`，或在菜单里输入编号。

第 17 项是通往下一个模块的热身：Socket、串口、Modbus TCP 里要用到的东西基本都在里面。

## 学完之后

下一个模块：Socket 与 TCP（对应教程 `03-Socket课程`），再往后是串口、Modbus TCP、S7（Sharp7）、OPC。

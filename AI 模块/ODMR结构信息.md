# ODMR-Lab 结构信息库（AI 记忆库）

> 本文件是 ODMR 程序的**结构参考/知识库**，供 AI 助手在思考、分析程序行为时查阅。
> - **读取**：`read-odmr-memory` 指令获取全部内容。
> - **维护**：AI 助手若发现本文件与实际程序不符，可用 `update-odmr-memory` 指令修改/补充（整文件替换，写操作需用户确认）。修改时务必保留仍正确的原有内容，只做增量修正。
> - 生成时间：2026-09-01

## 1. 程序总览
- ODMR-Lab：WPF .NET Framework 4.7.2（C# 7.3，x86）。光学检测磁共振（ODMR）/ NV 色心实验控制平台，涉及激光、微波、光子计数器（APD）、位移台、AFM 扫描台、锁相放大器、源表等设备。
- AI 控制：`AIService` 在 `localhost:5000`（仅本机回环）提供 HTTP 指令，格式 `?cmd=指令名&参数=值`（默认 GET；**`update-odmr-memory` 的完整 markdown 走 POST 请求体**，避免大内容触发约 16KB 请求行上限），统一 JSON 返回 `{success, data/message, time}`，反射 `[AiCommand]` 自动注册，safe/full 安全模式。
- 主窗口 `MainWindow`；实验页 `MainWindow.Exp_SequencePage`（含 `ExpObjects` 实验列表 + `CurrentExpObject` 当前实验对象）。

## 2. AI 服务（AIService）
- `AIService.cs`（partial 主类：HTTP 监听、反射注册、安全模式、系统指令）+ 分部文件：
  - .Experiments.cs 实验管理（list/select/get-params/set-param/start/stop/resume/status/outputs/buttons/click-button/read-exp-source）
  - `.Devices.cs` 设备控制（device-list/get/set、apd-sample、laser-on/off、camera-open/close、auto-connect）
  - `.Data.cs` 数据诊断（list-data-files/export-data/read-errlog/save-params/load-params）
  - `.UI.cs` 界面（window-control/list-pages/open-page）
  - `.Sequence.cs` 序列（list-seq-vars/set-seq-var）
  - 记忆库 2 条（在主类 `AIService.cs`）：`read-odmr-memory` 读本文件全文（GET）；`update-odmr-memory` 整文件替换（**content 走 POST 请求体**，safe 需 `confirm=true`，须先读原文保留正确部分）
- 本文件运行时位于 `exe目录\AI 模块\ODMR结构信息.md`，csproj 用 `PreserveNewest` 复制（源码未变则不覆盖 AI 的本地修改）。
- 安全模型：safe（默认）危险指令需 `confirm=true`；切 full 需 `confirm=yes`（AI 不得自行解除保护）。
- **永久禁止（任何模式都拦截）**：位移台/探针台等 `Target` 参数写入；名称含「移动」的实验按钮；AFM 下针（程序自带人工确认弹窗，AI 无法绕过）。
- 加新指令：任一 partial 文件写 `[AiCommand("名","描述","参数")] private string Xxx(Dictionary<string,string> args)`，反射自动注册；新文件需在 csproj 加 `<Compile Include>`。

## 3. 实验架构（核心）
- 基类 `ExperimentObject`（`实验类\ExperimentObject.cs`）：
  - 运行：`Start()` 起 `ExpThread` 后台线程；`Stop()` 软停止（设 `ThreadEndFlag`，在检查点退出）；`Resume()` 恢复暂停。
  - 进度：`SetProgress(0-100)` → 字段 `CurrentProgress` + UI 进度条；`GetProgress()` 读取（供 AI 查询）。
  - 状态文本：`SetExpState(state)` / `GetExpState()`（字符串，如「扫描中 17/40」）。
  - 事件：`InitEvent` / `ResumeStateEvent` / `EndStateEvent` / `ErrorStateEvent`。
  - 设备占用：`DeviceDispatcher.UseDevices` / `EndUseDevices`。
- 生命周期（`ExpThread` 内）：`ReadConfig()`（从界面面板读值）→ `GetDevices()+UseDevices` → `InitEvent` → `SetStartTime` → **`ExperimentEvent()`**（真正的实验逻辑；开始时清空 `SavedFileName`）→ `SetStopState()`（触发 `EndStateEvent`=SaveFile，置 `IsExpEnd=true`）→ `EndUseDevices` → `SetEndTime`。**成功、异常、软停止三条路径都走 `SetStopState`**。
- `ODMRExpObject`（实验包装/具体实验基类）：`ODMRExperimentName`/`ODMRExperimentGroupName`、`Description`、`InputParams`/`OutputParams`（`ParamB` 列表）、`DeviceList`、`InterativeButtons`（交互按钮字典 name→Action）、`ParentPage`、`D1FitDatas`、`IsAFMScanExperiment(e)`。
- 交互按钮：实验页 `ButtonsPanel` 的 `DecoratedButton`，按 `Text` 匹配；`click-exp-button` 走 `ButtonClickEvent`，找不到控件则在新后台线程直接调 Action。

## 4. 设备模型（统一寻址）
- `DeviceTypes` 枚举（中文）：相机、翻转镜、源表、位移台、探针位移台、样品位移台、微波位移台、镜头位移台、磁铁位移台、AFM扫描台、温控、信号发生器通道、锁相放大器、光子计数器、PulseBlaster、开关、电源。
- 寻址：`type=设备类型` + `desc=设备描述`（同类型多台时必填）+ `prop=参数名/描述`。
- `device-get` 纯读、不占设备（实验运行中也可用）；`device-set` 写前独占设备（运行中实验会拒绝）；位移台 `Target` 永久禁止。

## 5. 数据文件与保存
- 格式：`.userdat`（FileObject 语法，★/❤/☯/■/● 限定符）；权威说明见源码 `AI 模块\ODMR数据文件格式报告.md`（该文档不一定随安装包分发）。
- 自动保存：实验结束（`SetStopState`→`SaveFile`）时存到 `保存路径\组名\实验名\实验名+时间戳.userdat`；「保存路径」来自实验页设置；记录在实验对象 `SavedFilePath`/`SavedFileName`（`ExperimentEvent` 开始时清空 `SavedFileName`，故**结束后非空 = 最近一次运行**；手动「保存实验文件」按钮同样会更新这两个字段）。
- **`exp-status` / `get-exp-outputs` 的 `dataFile` 字段**：实验结束后返回该文件完整路径（自动保存关闭 / 未设保存路径 / 保存失败时为 null）。AI 应直接用 `export-data file=<dataFile>` 导出，无需再 `list-data-files` 查找。

## 6. 实验进度反馈
- **`exp-status` / `app-status` 的 `progress` 字段**：0-100 进度条百分比，来自 `GetProgress()`，与界面进度条同源，扫描实验实时更新。
- **约定（重要）**：AI **不主动轮询**；**用户询问进度时才查 `exp-status`**；且**仅当进度相比上次汇报有变化时才向用户播报**（不要重复说同一数值）。

## 7. 序列 / 全局脉冲
- `GlobalPulses.userdat`：全局脉冲表，键 `{脉冲名}→{IsLocked}★{长度ns}`；序列文件里峰名命中此表的段，运行时以表中长度覆盖。
- `list-seq-vars` / `set-seq-var` 读写；只能改已有脉冲长度，不能新增/删除；实验运行中禁止修改（并发读写保护）。

## 8. 安全约束（重要）
- 移动位移台/扫描台等机械移动 = 危险操作，只能人工在界面执行；AI 指令永久拦截（`device-set` 位移台 `Target`、`click-exp-button` 名称含「移动」的按钮）。
- AFM 下针等物理接触操作：程序自带人工确认弹窗，AI 不能绕过。
- 默认 safe 模式；危险操作（写设备 / 开激光 / 开相机 / 自动连接 / 点实验按钮 / 改序列 / 启动 AFM 类实验）需 `confirm=true`。
- `estop` / `stop-experiment` / `laser-off` / `camera-close` 及所有查询类指令永远允许。

## 9. read-exp-source 指令详情
- **功能**：读取 ODMR 程序的 C# 源码（从源码文件或反编译程序集）。
- **参数**：
  - 不带参数：读取当前选定的实验类源码（需先 select-exp）。
  - `class=<类名>`：按类名读取任意类，支持部分匹配（如 `class=CW` 匹配 `TotalCW`），优先匹配实验类。
  - `list=1`：列出源码目录和运行中程序集（ODMR_Lab 命名空间）的所有可用类，按类名排序，最多返回 1000 个。
  - `only=1`：只返回具体实验类（不含基类）。
- **查找逻辑**：先精确匹配程序集中的类型 → 部分匹配（类名包含指定字符串）→ 扫描源码目录的 .cs 文件。
- **源码目录**：自动从运行中程序集路径推断（向上查找 `D:\C#Codes\ODMR-Lab\`）。

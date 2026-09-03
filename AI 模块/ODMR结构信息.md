# ODMR-Lab 结构信息库（AI 记忆库）

> 本文件是 ODMR 程序的**结构参考/知识库**，供 AI 助手在思考、分析程序行为时查阅。
> - **读取**：`read-odmr-memory` 指令获取全部内容。
> - **维护**：AI 助手若发现本文件与实际程序不符，可用 `update-odmr-memory` 指令修改/补充（整文件替换，写操作需用户确认）。修改时务必保留仍正确的原有内容，只做增量修正。
> - 生成时间：2026-09-01
>
> ### ⚠️ 双文件同步规则（重要）
> 本记忆库存在**两份完全相同的副本**：
> - **源码目录**：`<项目根>\AI 模块\ODMR结构信息.md`（权威源，重新编译/部署时可能覆盖运行目录）
> - **运行目录**：`<项目根>\bin\x64\Debug\AI 模块\ODMR结构信息.md`（程序运行时实际读取的）
>
> **修改流程**：
> 1. 先用 `update-odmr-memory` 更新运行目录的副本（程序立即生效）
> 2. 再用文件复制命令将运行目录的内容**同步覆盖到源码目录**，防止下次编译/部署时被旧版覆盖
> 3. 两个文件内容必须始终保持一致

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
- 请求参数编码：2026-09-02 修复后，服务端从原始查询串（`req.Url.Query`）按 UTF-8 手动百分号解码（`SplitQuery`/`UrlDecodeUtf8`），客户端按 HTTP 标准 UTF-8 编码中文参数即可正确传递；大内容（如 update-odmr-memory 的 content）走 POST body（UTF-8），避免 HTTP.sys 请求行长度上限。旧版曾用 `req.Url.AbsolutePath` 导致 cmd/参数解析为空，勿回退。
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
- `ODMRExpObject`（实验包装/具体实验基类）：`ODMRExperimentName`/`ODMRExperimentGroupName`/`Description`、`InputParams`/`OutputParams`（`ParamB` 列表）、`DeviceList`、`InterativeButtons`（交互按钮字典 name→Action）、`ParentPage`、`D1FitDatas`、`IsAFMScanExperiment(e)`。
- 交互按钮：实验页 `ButtonsPanel` 的 `DecoratedButton`，按 `Text` 匹配；`click-exp-button` 走 `ButtonClickEvent`，找不到控件时在新后台线程直接调 Action。

## 4. 设备模型（统一寻址）
- `DeviceTypes` 枚举（中文）：相机、翻转镜、源表、位移台、探针位移台、样品位移台、微波位移台、镜头位移台、磁铁位移台、AFM扫描台、温控、信号发生器通道、锁相放大器、光子计数器、PulseBlaster、开关、电源。
- 寻址：`type=设备类型` + `desc=设备描述`（同类型多台时必填）+ `prop=参数名/描述`。
- `device-get` 纯读、不占设备（实验运行中也可用）；`device-set` 写前独占设备（运行中实验会拒绝）；位移台 `Target` 永久禁止。

## 5. 数据文件与保存
- 格式：`.userdat`（FileObject 语法，★/❤/☯/■/● 限定符）；权威说明见源码 `AI 模块\ODMR数据文件格式报告.md`（该文档不一定随安装包分发）。
- 自动保存：实验结束（`SetStopState`→`SaveFile`）时存到 `保存路径\组名\实验名\实验名+时间戳.userdat`；「保存路径」来自实验页设置；记录在实验对象 `SavedFilePath`/`SavedFileName`（`ExperimentEvent` 开始时清空 `SavedFileName`，故**结束后非空 = 最近一次运行**；手动「保存实验文件」按钮同样会更新这两个字段）。
- **`exp-status` / `get-exp-outputs` 的 `dataFile` 字段**：实验结束后返回该文件完整路径（自动保存关闭 / 未设保存路径 / 保存失败时为 null）。AI 应直接用 `export-data file=<dataFile>` 导出，无需再 `list-data-files` 查找。

## 6. 实验进度反馈与执行模式
- **`exp-status` / `app-status` 的 `progress` 字段**：0-100 进度条百分比，来自 `GetProgress()`，与界面进度条同源，扫描实验实时更新。

### 6.1 执行模式（重要）
AI 必须从用户的首条指令推断完整执行链，根据意图选择模式：

#### 端到端模式
用户说「执行/跑一下 XX 实验」「启动 XX 并告诉我结果」「执行 XX，返回结果」等：
- 流程：select-exp → start-experiment → **自动轮询 exp-status 直到 running=false** → get-exp-outputs → 一次性汇报最终结果
- 轮询策略：启动后每隔 3~5 秒调用一次 exp-status，检查 running 字段
- 轮询期间不向用户输出中间状态（除非实验耗时超过 60 秒，可简要提示"实验仍在运行中，请稍候"）
- 实验结束后立即调用 get-exp-outputs 获取输出结果，一次性向用户汇报

#### 仅启动模式
用户说「启动实验」「开始跑」等，未要求返回结果：
- 启动后告知用户已启动即可，不主动轮询

#### 进度查询模式
用户问「进度怎样了」「跑完了吗」：
- 查一次 exp-status 并汇报

#### 含后续操作模式
用户说「执行 XX，导出/画图」「执行 XX，返回结果，导出图」：
- 端到端执行 + 自动 export-data + 绘图，一次性完成

### 6.2 轮询约定
- 端到端模式下，AI 自动轮询直到实验完成，无需用户反复催促
- 仅当进度相比上次汇报有变化时才向用户播报中间状态

### 6.3 连续实验模式（重要，2026-09-03 新增）
当用户要求执行多个连续实验（如「定位后做 CW 谱，然后测 Rabi」或隐含的实验链）时：

**核心规则**：
1. **自动衔接**：完成一个实验后，立即开始下一个实验，**禁止中途停止输出或等待用户指示**
2. **完整执行链**：从第一个实验到最后一个实验，一气呵成完成整个流程
3. **统一汇报**：所有实验完成后，一次性汇报所有结果

**执行流程**：
```
实验1: select-exp → start-experiment → 轮询到完成 → 获取结果
↓（自动衔接，不停顿）
实验2: select-exp → start-experiment → 轮询到完成 → 获取结果
↓（自动衔接，不停顿）
实验N: select-exp → start-experiment → 轮询到完成 → 获取结果
↓
一次性汇报所有实验结果
```

**禁止行为**：
- ❌ 完成实验1后停下来问用户「是否继续实验2？」
- ❌ 完成实验1后输出结果，然后等待用户下一步指令
- ❌ 在实验链中间输出「接下来要做实验2」但不执行

**正确行为**：
- ✅ 自动完成整个实验链，最后统一汇报
- ✅ 如果某个实验失败，记录错误并继续后续实验，最后汇报所有结果（包括失败信息）

### 6.4 参数批量设置规则（重要，2026-09-03 新增并修订）
当需要修改多个实验参数时：

**核心规则**：
1. **批量收集**：先收集所有需要修改的参数
2. **危险操作确认**：将所有待修改参数通过**第一个 set-exp-param 调用**以 `risk_level=dangerous` + `confirm=true` 的形式触发界面确认框
3. **自动继续**：用户在界面点击确认后，AI 立即继续设置剩余参数并启动实验，无需再次询问

**执行流程**：
```
1. get-exp-params 获取当前参数
2. 确定需要修改的参数列表
3. 调用第一个 set-exp-param，设置 risk_level=dangerous，explanation 中列出所有待修改参数：
   例如：
   - command: set-exp-param
   - args: {param: "Input_RFFrequency", value: "2871", confirm: "true"}
   - risk_level: dangerous
   - explanation: "将修改以下参数并启动实验：微波频率=2871 MHz，微波功率=12 dBm，循环次数=1000"
4. 用户在界面看到确认框，点击确认
5. 用户确认后，AI 立即：
   - 调用 set-exp-param 设置剩余参数（无需再次确认，因为已通过第一个确认框获得授权）
   - 调用 start-experiment 启动实验
```

**禁止行为**：
- ❌ 逐个设置参数，每个参数都让用户确认一次
- ❌ 设置一个参数后等待用户反馈再设置下一个
- ❌ 用文字描述参数列表让用户在聊天中回复"确认"（应通过界面危险操作确认框）

**正确行为**：
- ✅ 通过第一个 set-exp-param 的 dangerous 确认框一次性通知用户所有参数修改
- ✅ 用户点击确认后，自动完成剩余参数设置和实验启动
- ✅ 确认框的 explanation 字段清晰列出所有待修改参数

## 7. 序列 / 全局脉冲
- `GlobalPulses.userdat`：全局脉冲表，键 `{脉冲名}→{IsLocked}★{长度ns}`；序列文件里峰名命中此表的段，运行时以表中长度覆盖。
- `list-seq-vars` / `set-seq-var` 读写；只能改已有脉冲长度，不能新增/删除；实验运行中禁止修改（并发读写保护）。

## 8. 安全约束（重要）
- 移动位移台/扫描台等机械移动 = 危险操作，只能人工在界面执行；AI 指令永久拦截（`device-set` 位移台 `Target`、`click-exp-button` 名称含「移动」的按钮）。
- AFM 下针等物理接触操作：程序自带人工确认弹窗，AI 不能绕过。
- 默认 safe 模式；危险操作（写设备 / 开激光 / 开相机 / 自动连接 / 点实验按钮 / 改序列 / 启动 AFM 类实验）需 `confirm=true`。
- `estop` / `stop-experiment` / `laser-off` / `camera-close` 及所有查询类指令永远允许。

## 9. 输出规范
- 实验完成后直接输出结果，**禁止**用「如需进一步分析请告诉我」「是否需要导出 CSV」等废话结尾
- 用户首条指令已包含「返回结果」「导出」「画图」等关键词时，AI 应在实验结束后自动执行后续步骤，不要等用户再说一遍
- 用户已明确表达意图时，禁止在中间步骤反问「是否确认？」「是否需要导出？」等

## 10. 实验参数约束（强制检查，2026-09-03 修订）

> **⚠️ 本节为强制约束，AI 在启动实验前必须执行检查，不得跳过！**

### 10.1 序列循环次数（SeqLoopCount）—— 必须检查

**合理范围参考**：
- **CW 谱实验**：推荐 1000 次左右
  - ⚠️ 低于 100 次：光子数可能严重不足，谱峰对比度差，难以识别
- **Rabi 实验**：通常 100,000 - 1,000,000 次
  - ⚠️ 低于 10,000 次：信号噪声比可能不足

**注意**：循环次数无上限限制，用户可根据需要设置任意值。

### 10.2 微波功率（RFAmplitude）—— 必须检查

**硬性要求**：
- **必须** ≤ 15 dBm
- ⚠️ 超过 15 dBm：可能损坏设备或导致样品加热，**必须警告用户**
- 典型工作范围：5 - 15 dBm

### 10.3 AI 强制检查流程（不得跳过）

**执行时机**：在调用 `start-experiment` 之前，**必须**执行以下检查：

```
步骤1: get-exp-params 获取当前参数
步骤2: 检查循环次数是否在合理范围（见 10.1）
步骤3: 检查微波功率是否 ≤ 15 dBm（见 10.2）
步骤4: 若任一参数异常：
   - 必须在启动实验前警告用户，等待用户确认
步骤5: 用户确认后，才能调用 start-experiment
```

**参数异常时的处理方式**：

**必须警告用户并等待确认**：
```
1. 发现 Input_LoopCount = 1（CW谱），低于推荐值 1000
2. 在聊天中向用户明确警告：
   "⚠️ 当前循环次数为 1 次，CW 谱推荐 1000 次左右。循环次数过低会导致光子数不足、谱峰对比度差。是否仍要以当前参数启动实验？"
3. 等待用户在聊天中明确回复确认
4. 用户确认后继续启动
```

**禁止行为**：
- ❌ 跳过参数检查直接启动实验
- ❌ 发现参数异常但不警告用户
- ❌ 自动修改参数而不通知用户
- ❌ 在用户未确认的情况下启动实验

**正确行为**：
- ✅ 发现异常时，在聊天中明确警告用户具体问题和可能后果
- ✅ 等待用户在聊天中明确回复确认后，才能启动
- ✅ 用户确认后可以修改参数或直接以当前参数启动

### 10.4 检查清单（AI 启动前必须逐项确认）

启动实验前，AI 必须在内部确认以下检查项：
- [ ] 循环次数是否在合理范围？若异常，是否已警告用户并获得确认？
- [ ] 微波功率是否 ≤ 15 dBm？若超标，是否已警告用户并获得确认？

**只有全部检查通过（参数正常 或 用户已确认），才能调用 start-experiment。**

## 11. 用户告知的实验配置备忘（跨对话长期有效）

> 本节记录**用户口头确认**的设备/通道配置（实验相关 vs 无关）。读写设备参数、监测、汇报时只针对「实验相关」项；未列出的通道默认与实验无关，不必读取/汇报/监测，除非用户另行指示。后续用户告知的同类配置按相同格式追加到本节。

### 11.1 温控 SRS PTC10（SN 153027，共 21 通道）
- **实验相关（仅 3 个通道，2026-09-02 用户确认）**：
  - `Out 1`：PID 控温输出通道（控制目标本身；参数含 SetPoint 设定温度 / Ramp 变温速率 / P / I / D / Power 输出功率 / IOType / PIDMode / 功率上下限）。
  - `2A`：被控点温度测量通道（控温对象的实际温度）。
  - `2B`：实验相关温度测量通道。
- **与实验无关（默认不处理，共 18 个通道）**：I 1、V 1、R 1、PCB 1、PCB 2、PCB 3、Out 3、Vmon 3、3B、5A、5B、5C、5D、DIO、Relays、V1、V2、V3。
  - 备注：V2 / V3 / 3B 读数「非数字」（未接传感器或未配置）；DIO / Relays 为数字量通道；Out 3 / Vmon 3 为监测输出。这些读数异常属正常，无需报警。

# ODMR-Lab AI 控制指令说明

> 供 AI Agent 通过 HTTP 控制 ODMR-Lab 程序的全部指令文档。
> 生成时间:2026-09-01 · 指令总数:37 条 · 全部编译通过

## 1. 服务概览

- **地址**:`http://localhost:5000/`(仅允许本机回环访问,外部访问返回 403)
- **协议**:GET 请求,参数全部走 query string,`cmd` 为指令名,其余为参数
- **响应**:统一 JSON `{ "success": true/false, "data"/"message": ..., "time": "..." }`
- **调用示例**:
  ```
  http://localhost:5000/?cmd=help
  http://localhost:5000/?cmd=set-exp-param&param=输入_激光功率&value=500
  ```
- **程序启动即自动启动 AI 服务**(MainWindow 构造时 `_ai.Start()`)

## 2. 安全模型(重要)

服务有 `safeMode` 字段,两个档位:

| 模式 | 行为 |
|---|---|
| `safe`(默认) | 危险指令必须带 `confirm=true` 才执行 |
| `full` | 解除确认限制(完全信任 AI) |

- 切 full 必须 `set-safe-mode mode=full confirm=yes`(AI 不能自行解除保护);切回 safe 无需确认。
- 模式**只存内存**,程序重启恢复 safe。
- **永久禁止(任何模式都拦截)**:
  - 位移台 `Target` 参数写入(7 类位移台全覆盖)——移动位移台必须人工在界面操作;
  - 名称含「移动」的实验按钮(移动位移台/移动扫描台到指定位置/移动到选定位置)——`click-exp-button` 对这些按钮永久拦截;
  - AFM 实验下针——程序自带人工确认弹窗,AI 无法绕过。
- **需要 confirm=true 的操作(safe 模式)**:`device-set`、`laser-on`、`camera-open`、`auto-connect`、`click-exp-button`、`set-seq-var`、启动 AFM/下针类实验。
- **永远允许**:`estop`、`stop-experiment`、`laser-off`、`camera-close`、所有查询类指令(含 `list-exp-buttons`/`list-seq-vars`/`read-exp-source`)。

## 3. 指令清单

### 3.1 系统指令(6)

| 指令 | 参数 | 说明 |
|---|---|---|
| `ping` | 无 | 心跳,返回 pong |
| `help` | 无 | 列出全部指令+参数说明,含当前 safeMode |
| `get-logs` | 无 | 最近错误日志(100 条环形缓存) |
| `app-status` | 无 | 当前页面/当前实验/运行状态/安全模式 |
| `set-safe-mode` | `mode=safe\|full`,full 需 `confirm=yes` | 切换安全模式 |
| `estop` | 无 | 紧急停止:有运行实验则停止之,否则关激光 |

### 3.2 界面控制指令(3)

| 指令 | 参数 | 说明 |
|---|---|---|
| `window-control` | `action=maximize\|minimize\|restore\|activate` | 主窗口最大化/最小化/还原/激活到前台(activate 会自动先还原最小化) |
| `list-pages` | 无 | 列出可用页面名 |
| `open-page` | `page=<页面名>` | 打开指定页面,复用界面按钮点击逻辑(菜单高亮同步)。可用名:其他设备、位移台、相机、光子计数器、设备参数监测、设备参数设置、Trace、序列编辑器、ODMR实验、位移台控制界面、样品定位、场效应器件测量、自定义算法、Python管理器、数据记录、数据、共享剪切板 |

### 3.3 实验管理指令(12)

| 指令 | 参数 | 说明 |
|---|---|---|
| `list-experiments` | 无 | 实验列表(index/name/group/desc/afm 标记) |
| `select-exp` | `index=` 或 `name=` | 选择当前实验(加载参数面板);运行中实验会拒绝切换 |
| `get-exp-params` | 无 | 输入参数(可改,含 name/desc/value/type)/输出参数/设备选择 |
| `set-exp-param` | `param=` `value=` | 修改输入参数;会同步写回界面面板(实验启动时 ReadConfig 从面板读值);运行中禁止 |
| `start-experiment` | AFM 类需 `confirm=true` | 后台线程异步启动,立即返回;AFM 类实验会弹人工确认框 |
| `exp-status` | 无 | 运行状态(running/paused/state/error) + `dataFile`:实验结束后返回已保存的数据文件完整路径(自动保存时存于 `保存路径\组名\实验名\实验名+时间戳.userdat`,路径来自实验页「保存路径」设置);运行中/自动保存关闭时为 null |
| `stop-experiment` | 无 | 软停止,实验在检查点退出并自动释放设备、保存数据文件,永远允许;停止后轮询 exp-status,`dataFile` 即返回文件路径 |
| `resume-experiment` | 无 | 恢复已暂停实验 |
| `get-exp-outputs` | 无 | 输出参数值 + 拟合信息(公式/组) + `dataFile`:本次运行已保存的数据文件路径 |
| `list-exp-buttons` | 无 | 当前实验的交互按钮(页面顶部按钮栏,信息来自 `InterativeButtons`);返回每个按钮 `name`/`blocked`(含「移动」的永久禁止)/提示;需先 `select-exp` |
| `click-exp-button` | `button=<按钮名>`,safe 需 `confirm=true` | 点击当前实验的交互按钮(等同人工点击,在其后台线程执行);实验运行中禁止;按钮名含「移动」的(移动位移台/扫描台/选定位置)永久禁止,只能人工在界面执行;部分按钮会弹出窗口(参数设置/文件选择/标定等),需人工在窗口内完成 |
| `read-exp-source` | `srcdir=`(可省)`only=1`(只取具体类) | 读取当前实验的 C# 代码,三级回退:① 有源码目录时沿继承链(派生类→基类)查找源文件返回内容(超 20 万字符截断);② 打包安装无源码时自动用 ILSpy 引擎(ICSharpCode.Decompiler 11)从运行中的程序集**反编译**出 C# 代码(单类 8 万字符截断、总计 25 万,`sources` 数组按 具体类+基类链 返回);③ 反编译也不可用时降级为反射结构概览(全部方法名/签名/可见性)。自动从 exe 目录向上找 `.csproj` 定位源码根 |

### 3.4 设备控制指令(9,统一寻址)

设备参数读写统一为一对指令,寻址方式:`type=<DeviceTypes 枚举名>` + `desc=<设备描述>` + `prop=<参数名或描述>`。

| 指令 | 参数 | 说明 |
|---|---|---|
| `device-list` | `type=`(缺省全部)`params=1` | 设备列表+占用状态;`params=1` 同时列出每个设备可读写参数 |
| `device-get` | `type` `desc` `prop` | 读参数值,纯读不占设备,实验运行中也可用 |
| `device-set` | `type` `desc` `prop` `value` `confirm=true` | 写参数(safe 需确认);写前独占设备,运行中实验会拒绝;位移台 Target 永久禁止 |
| `apd-sample` | `desc`(单台可省)`ms`(默认 3000) | APD 光子计数率采样,判断探针是否在 NV 上;会独占 APD |
| `laser-on` | `duty`(0-1)`desc`(可省)`confirm=true` | 开激光(PulseBlaster 通道 5,300ms 脉冲) |
| `laser-off` | `desc`(可省) | 关激光,永远允许 |
| `camera-open` | `desc`(单台可省)`confirm=true` | 打开相机实时预览窗口(窗口会一直占用相机);相机被实验占用时拒绝 |
| `camera-close` | `desc`(单台可省) | 关闭相机预览窗口并释放相机,永远允许 |
| `auto-connect` | 无(safe 需 `confirm=true`) | 重新搜索并连接全部已配置设备,等同主界面「自动连接」按钮;实验运行中或任何设备被占用(如相机预览开着)时拒绝;请求可能阻塞 30~60 秒,调用方超时请设 120 秒以上 |

DeviceTypes 枚举值(中文):相机、翻转镜、源表、位移台、探针位移台、样品位移台、微波位移台、镜头位移台、磁铁位移台、AFM扫描台、温控、信号发生器通道、锁相放大器、光子计数器、PulseBlaster、开关、电源。
多台同类型设备时 `desc` 必填(缺失会返回可用列表)。

### 3.5 数据与诊断指令(5)

| 指令 | 参数 | 说明 |
|---|---|---|
| `list-data-files` | `dir`(默认程序目录)`extype` | 列 `.userdat` 数据文件(最多最近 200 个) |
| `export-data` | `file` `outcsv`(默认同名 .csv) | userdat→CSV:头部注释含实验/输入/输出/设备参数;1D 曲线 `index,value` 行;2D 曲面每行一个 Y 扫描(前两列 Y 序号/Y 值,其余为 Z) |
| `read-errlog` | `lines`(默认 50,最大 1000) | 程序异常日志 errlog.txt 末尾 N 行 |
| `save-params` | 无 | 保存全部界面参数(等同关闭时保存) |
| `load-params` | 无 | 从文件恢复全部界面参数(会覆盖当前值) |

### 3.6 序列指令(2)

序列中脉冲段的真实运行长度由全局脉冲表 `Sequences\GlobalPulses.userdat` 决定:序列文件里峰名命中该表的段,运行时以表中长度覆盖。

| 指令 | 参数 | 说明 |
|---|---|---|
| `list-seq-vars` | 无 | 全部全局脉冲变量:`name`(脉冲名)/`length`(长度,ns)/`locked`(锁定标志,仅表示删除时是否警告,不影响长度修改) |
| `set-seq-var` | `name=<脉冲名>` `length=<非负整数,ns>`,safe 需 `confirm=true` | 设置全局脉冲长度,自动写入 `GlobalPulses.userdat`,含该脉冲的序列下次运行即生效;实验运行中禁止修改(并发读写保护) |

## 4. 典型工作流

```bash
# 0. 检查服务与状态
curl "http://localhost:5000/?cmd=app-status"

# 1. 选实验 → 看参数 → 改参数 → 启动
curl "http://localhost:5000/?cmd=list-experiments"
curl "http://localhost:5000/?cmd=select-exp&index=0"
curl "http://localhost:5000/?cmd=get-exp-params"
curl "http://localhost:5000/?cmd=set-exp-param&param=<name字段>&value=500"
curl "http://localhost:5000/?cmd=start-experiment"

# 2. 轮询进度 → 读结果 → 导出 CSV(实验结束后 exp-status 的 dataFile 返回数据文件路径)
curl "http://localhost:5000/?cmd=exp-status"
curl "http://localhost:5000/?cmd=get-exp-outputs"
curl "http://localhost:5000/?cmd=export-data&file=<exp-status 返回的 dataFile>"

# 3. 设备读写示例
curl "http://localhost:5000/?cmd=device-list&type=锁相放大器&params=1"
curl "http://localhost:5000/?cmd=device-get&type=锁相放大器&prop=频率"
curl "http://localhost:5000/?cmd=device-set&type=源表&prop=电流&value=0.5&confirm=true"

# 4. 激光 / 急停
curl "http://localhost:5000/?cmd=laser-on&duty=1&confirm=true"
curl "http://localhost:5000/?cmd=laser-off"
curl "http://localhost:5000/?cmd=estop"

# 5. 相机预览 / 设备重连
curl "http://localhost:5000/?cmd=camera-open&confirm=true"
curl "http://localhost:5000/?cmd=camera-close"
curl "http://localhost:5000/?cmd=auto-connect&confirm=true"   # 可能耗时 30~60 秒

# 6. 界面操作
curl "http://localhost:5000/?cmd=open-page&page=ODMR实验"
curl "http://localhost:5000/?cmd=window-control&action=maximize"

# 7. 实验按钮 / 源代码 / 序列脉冲变量
curl "http://localhost:5000/?cmd=list-exp-buttons"
curl "http://localhost:5000/?cmd=click-exp-button&button=设置全局脉冲参数&confirm=true"
curl "http://localhost:5000/?cmd=read-exp-source"
curl "http://localhost:5000/?cmd=list-seq-vars"
curl "http://localhost:5000/?cmd=set-seq-var&name=RabiTime&length=300&confirm=true"
```

## 5. 代码位置与构建

| 文件 | 内容 |
|---|---|
| `AI 模块/AIService.cs` | 主类:HTTP 服务、反射注册、安全模式、系统指令 |
| `AI 模块/AIService.Experiments.cs` | 实验管理 12 条(含 list-exp-buttons / click-exp-button / read-exp-source) |
| `AI 模块/AIService.Devices.cs` | 设备控制 9 条(含相机预览/自动连接) |
| `AI 模块/AIService.Data.cs` | 数据诊断 5 条 |
| `AI 模块/AIService.UI.cs` | 界面控制 3 条 |
| `AI 模块/AIService.Sequence.cs` | 序列全局脉冲变量 2 条(list-seq-vars / set-seq-var) |
| `AI 模块/ODMR数据文件格式报告.md` | `.userdat` 全格式说明(FileObject 语法/序列/全局脉冲表/实验结果/ODMRConfig/笔记/自定义数据),供编写解析 Skill 用 |
| `MainWindow.xaml.cs` | 新增 `OpenPage()` 与 `AIPageNames`(复用界面按钮逻辑) |

**加新指令**:在任一 partial 文件写 `[AiCommand("名字", "描述", "参数说明")] private string Xxx(Dictionary<string,string> args)` 即可,反射自动注册;若是新文件需在 `ODMR Lab.csproj` 加 `<Compile Include>`。

**反编译依赖**:`read-exp-source` 打包安装路径用到 ILSpy 反编译引擎 `ICSharpCode.Decompiler 11.0.0.9375`(netstandard2.0)+ `System.Collections.Immutable 9.0.0` / `System.Reflection.Metadata 9.0.0`(net462 版),在 `packages\` 目录、csproj 与 packages.config 均已登记;SRM 请求的 System.Memory 4.0.1.2 由 App.config 已有的 `0.0.0.0-4.0.5.0` 绑定重定向覆盖,无需新增。

**构建**(dotnet build 不可用,必须用 VS MSBuild;Git Bash 下参数用 `-p:` 不能用 `/p:`):
```
"F:/Visual Studio/MSBuild/Current/Bin/MSBuild.exe" "ODMR Lab.csproj" -p:Configuration=Debug -v:m -nologo
```
语言版本 C# 7.3(无 using 声明、无 C#9 混合类型条件表达式)。

**回滚**:git 基线 `174b86b`(master);物理备份 `D:\C#Codes\ODMR-Lab-备份-20260901\`。

## 6. 已知限制 / 下一步建议

- `export-data` 不导出时间序列(TimeChartData1D)曲线,只导出数值 1D/2D;
- `apd-sample` 请求会阻塞 ~ms+5s(内部计数窗口),调用方注意超时;
- `auto-connect` 会重扫全部设备类型,请求可能阻塞 30~60 秒,调用方超时请设 120 秒以上;
- `camera-open` 打开的预览窗口会一直占用相机直到 `camera-close` 关窗,运行需要相机的实验(如 AFM 扫描)前必须先关窗;
- `read-exp-source` 有源码目录时返回的是**源码文件**,与 exe 不同步时内容可能滞后于实际运行行为;打包安装时自动反编译运行中的程序集,返回的就是**实际运行的代码**(最准确),但反编译大类的请求可能耗时数秒;`only=1` 可只取具体类减少返回量;
- `click-exp-button` 返回时按钮操作可能仍在后台线程执行;若按钮弹窗口(参数设置/文件选择等),AI 无法代替窗口内输入,需人工完成;
- `set-seq-var` 只改全局脉冲表中已有脉冲的长度,不能新增/删除脉冲(避免破坏序列合法性);
- 建议实测一轮:`help → app-status → list-pages → open-page → list-experiments → select-exp → get-exp-params → list-exp-buttons → start-experiment → exp-status → get-exp-outputs`;
- 后续可扩展:实验参数批量设置、多实验排队、CSV 导出加 X 轴实际值列、指令执行历史查询。

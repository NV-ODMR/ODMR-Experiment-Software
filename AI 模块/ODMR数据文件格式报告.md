# ODMR-Lab 数据文件格式报告

> 用途：供另一个 Agent 编写「ODMR 文件解析 Skill」时作为权威格式参考。
> 所有结论均来自源码（ODMR-Lab、CodeHelper、Controls 三个项目），关键位置已标注。
> 源码位置：
> - 通用文件类：`D:\C#Codes\CodeHelper\FileObject.cs`（FileObject / FileData / DataPoint）
> - 序列读写：`D:\C#Codes\ODMR-Lab\实验部分\序列编辑器\序列通道类\SequenceDataAssemble.cs`、`GroupSequenceWaveSeg.cs`、`SingleSequenceWaveSeg.cs`、`SequenceChannelID.cs`
> - 全局脉冲表：`D:\C#Codes\ODMR-Lab\实验部分\序列编辑器\GlobalPulseParams.cs`
> - 实验结果文件：`D:\C#Codes\ODMR-Lab\实验部分\ODMR实验\参数\SequenceFileExpObject.cs`、`实验类\ExperimentObject.cs`
> - 实验参数配置：`D:\C#Codes\ODMR-Lab\实验部分\ODMR实验\参数\SequenceExpObject.cs`（ReadFromPageAndWriteConfigToFile）
> - 数据记录(笔记)：`D:\C#Codes\ODMR-Lab\扩展部分\数据记录\记录系统\Note.cs / NoteUnit.cs`、`界面及子窗口\NoteHelper.cs`
> - 自定义数据：`D:\C#Codes\ODMR-Lab\数据处理\UserCustomFileObject.cs`

---

## 1. 文件清单总览

所有持久化数据统一使用 `.userdat` 扩展名，全部基于第 2 节的 FileObject 通用语法。相对路径均相对于**程序运行目录**（`Environment.CurrentDirectory`，即 exe 所在目录；用户另存的结果文件除外）。

| 文件/目录 | 内容 | 写入方 |
|---|---|---|
| `Sequences\{序列名}.userdat` | 脉冲序列定义 | 序列编辑器 |
| `SequenceGroup\{组合名}.userdat` | 序列组合（可被序列引用的子序列） | 序列编辑器 |
| `Sequences\GlobalPulses.userdat` | 全局脉冲长度表 | 各实验运行时自动维护 |
| `{用户保存根}\{实验分组}\{实验名}\{实验名}{时间戳}.userdat` | **实验结果文件**（参数+图表数据） | 实验结束自动/手动保存 |
| `ODMRConfig\{分组}_{实验名}.userdat` | 实验界面参数配置（启动时恢复） | 参数保存 |
| `UIParam\Param.userdat` | 全局 UI/设备参数 | 参数保存 |
| `note{N}\...`（数据记录扩展） | 实验记录笔记（标签+条目+附件） | 数据记录扩展 |
| 任意 `.userdat`（自定义数据） | 用户自定义数据文件 | 自定义数据模块 |

---

## 2. FileObject 通用语法（`.userdat` 基础层）

**编码与换行**：UTF-8（无 BOM），Windows 换行 `\r\n`。解析时按 `\n` 切分即可（`ReadFromFile` 先 `Trim()` 整个内容再 `Split('\n')`）。

**文件结构**（严格顺序）：

```
{Key}★{Value}                        ← 描述区行，任意多行（可为 0 行）
...
userdata description ending line     ← 描述区结束标记（固定字符串，独占一行）
data name line★{数据集1}★{数据集2}★...★  ← 数据名行；无数据集时该行就是 "data name line★"
{v1}★{v2}★...★{vn}                   ← 数据行，行数不限（可为 0 行），列数 = 数据集数
...
end of file                          ← 文件尾标记（必须，否则判定"文件格式损坏"）
```

**特殊字符（全部为限定符，内容中禁止出现）**：

| 字符 | Unicode | 作用 |
|---|---|---|
| `★` | U+2605 | 字段分隔符（描述行的 key/value、数据名行、数据行列间） |
| `❤` | U+2764 | 数据行中的缺失值占位（该数据集在此行没有值） |
| `☯` | U+262F | 点数据（DataPoint）内部数值分隔符 |
| `■` | U+25A0 | 转义字符：代表 `\n` |
| `●` | U+25CF | 转义字符：代表 `\r` |

**业务层逻辑分隔符（语法层不处理，仅键名/数据集名内部使用）**：

| 字符 | Unicode | 使用场景 |
|---|---|---|
| `→` | U+2192 | 序列/结果/配置文件键名内部分层，如 `Input→描述→属性名` |
| `♠` | U+2660 | 数据记录(笔记)系统标签数据集名内部分层 |
| `$` | U+0024 | 自定义数据文件数据集名内部分层（`名$组$轴类型`） |

**数据集（数据区列）**：
- 数据名行的数据集名按出现顺序对应数据行的各列；每行按 `★` 切分后**列数必须等于数据集数**，否则 C# 端抛 `FileLoadException("文件数据部分格式错误：数据不等长")`。
- 某列值为 `❤` 表示该数据集在此行缺值。
- 数据集元素类型按**首元素**推断（`JudgeDataType`，顺序判定）：
  1. 能 `double.Parse` → double（特殊字面量：`NaN` 或 `非数字` → NaN；`正无穷大` → +Inf；`负无穷大` → -Inf）
  2. 能 `bool.Parse` → bool（`True`/`False`）
  3. 含 `☯` → 点数据（`☯` 分隔的 double 数组）
  4. 能按 `yyyy:MMMM:dd:HH:mm:ss:FFF` 解析（CurrentCulture）→ DateTime，例：`2026:September:01:10:30:45:123`
  5. 其余 → string

**解析注意事项（坑）**：
1. 尾行校验：`Trim()` 后按 `\n` 切分的最后一行必须是 `end of file`。
2. 描述区键不可重复（C# 端用 `Dictionary.Add`，重复键直接抛异常 → 整个文件读取失败）。
3. 描述行若 value 中含 `★`，读取时只取 `split[0]`、`split[1]`，**value 会被第一个 `★` 截断**——写入端只校验了 key 不含限定符，没校验 value。
4. 描述行 value 允许为空字符串，key 不允许为空。
5. `■`/`●` 的转义在 key 和 value 上对称生效（读时还原为真实换行符）。
6. 日期格式中的月份是**完整月份名**且依赖写入时的区域语言环境（`CultureInfo.CurrentCulture`，如中文系统可能写 `2026:九月:01:...`），跨语言解析时要小心。
7. `SaveToFile` 会自动给不带 `.userdat` 后缀的路径补后缀。

**C# API 对照（供 Skill 设计功能面参考）**：
`FileObject.ReadFromFile(path)`、`ReadDescription(path)`（只读描述区）、`SaveToFile(path)`、`WriteDoubleData/WriteBooleanData/WriteStringData/WritePointData/WriteDateData(名, 数据)`、`ExtractDouble/ExtractBoolean/ExtractString/ExtractPoint/ExtractDate(名)`、`GetDataNames()`、`DataCount()`、`JudgeDataType(名)`。

---

## 3. 全局脉冲长度表 `Sequences\GlobalPulses.userdat`

- 无数据区（数据名行为 `data name line★`，数据行为 0 行）。
- 描述行键格式：`{脉冲名}→{True|False}`，值 = 脉冲长度（int，**单位 ns**）。
  - `→` 后是 `IsLocked`（锁定标志，bool）。**锁定只影响 UI 上删除时是否警告，不影响长度修改**——实验运行时会自由覆盖长度。
- 示例（真实文件）：
  ```
  RabiTime→True★278
  SpinEchoTime→True★427
  T2StarStep→True★10542
  CWSampleTime→True★250000
  3HalfPiX→False★221
  ```
- 语义：序列中 `PeakName` 命中本表的段，**运行时实际长度以本表为准**（`UpdateGlobalPulse` 覆盖）；未命中则用序列文件里写的 Spans。因此解析序列的"真实执行时长"必须先读本表。
- 该文件由各实验运行时自动 `WriteToFile`（`SetGlobalPulseLength` 每次修改都会落盘），是**读写文件**，Skill 若要写回需保持 `名→bool★长度` 键格式。

---

## 4. 序列定义文件 `Sequences\{序列名}.userdat`

文件名（不含扩展名）= 序列名（`SequenceAssembleName`，两处一致）。

**描述区**（固定 2 个键）：
```
SequenceAssembleName★{序列名}
SequenceAssembleLoopCount★{循环次数 int}
```
注意：实验运行时通常用代码覆盖 LoopCount（`sequence.LoopCount = sequenceLoopCount`），文件里的值只是保存时的快照。

**数据区**：列数 = `1 + 4×通道数`。第 1 列固定为 `ChannelNames`，之后每个通道 4 列。

| 数据集名 | 内容 |
|---|---|
| `ChannelNames` | 通道枚举名，取值 `Ch_0`…`Ch_19`（`SequenceChannel` 枚举，对应板卡物理通道） |
| `ChannelName→{ch}→PeakNames` | 该通道各峰（段）的名称，顺序即时间顺序；命中全局脉冲表的名字表示长度受其控制 |
| `ChannelName→{ch}→PeakValues` | 普通峰：`Zero`/`One`（该段此通道输出低/高电平）；组合峰：组内通道序号（int，字符串形式） |
| `ChannelName→{ch}→Spans` | 普通峰：段长度（int，**ns**）；组合峰：字面量 `SequenceGroup` |
| `ChannelName→{ch}→IsTrigger` | 普通峰：`True`/`False`；组合峰：字面量 `SequenceGroup` |

**组合峰（GroupSequenceWaveSeg）识别规则**：`IsTrigger == "SequenceGroup"`（此时 Spans 也是 `SequenceGroup`，PeakValues = 组内通道索引）。
- 组合峰的 `PeakNames` 列值 = 组合文件的名字（对应 `SequenceGroup\{该名字}.userdat`）。
- 组合峰的时长 = 组内被选中通道（PeakValues 指定的索引）所有峰长度之和（`PeakSpan` 计算属性）。
- 展开后该通道在该位置输出组内所选通道的波形。

**真实示例**（`bin\Debug\Sequences\CW.userdat`，3 通道 8 峰）：
```
SequenceAssembleName★CW
SequenceAssembleLoopCount★0
userdata description ending line
data name line★ChannelNames★ChannelName→Ch_3→PeakNames★ChannelName→Ch_3→PeakValues★ChannelName→Ch_3→Spans★ChannelName→Ch_3→IsTrigger★ChannelName→Ch_4→PeakNames★...
Ch_3★Custom★Zero★20★False★Custom★Zero★20★False★Custom★One★20★False
Ch_4★CountTrig★Zero★20★False★CountTrig★One★20★False★CountTrig★One★20★False
Ch_5★CWSampleTime★One★250000★False★CWSampleTime★Zero★250000★False★CWSampleTime★One★250000★False
❤★CountTrig★Zero★20★False★...   ← Ch_3 只有 3 峰，剩余行该列用 ❤ 占位
end of file
```
（注意各通道峰数可以不同，短通道用 `❤` 补齐行——这是 FileObject 的缺失值机制，解析时要还原成"该通道到第 3 峰结束"。）

**合法性约束**（写入/编译时校验，Skill 做校验时可复用）：
1. 各通道**展开组合峰后的总时长必须相等**（否则 `各个通道的总时间必须相同`）。
2. 各通道组合峰数量相同，且同一序号的组合峰在各通道的**起始时间相同**。
3. `IsTrigger=True` 的峰，其**前一峰必须名为 `TriggerWait`**。
4. 所有触发峰在各通道的起止时间必须完全一致。

**时基与编译语义**：
- 时间单位 **ns**（整数，对应板卡指令 `CommandLine.TimeLength` 注释"持续时间(ns)"）。
- 编译成板卡指令：扫描所有通道，找出所有 `One` 段和 Trigger 段的边界时间点；相邻两个边界间若存在触发段则生成 Trigger 指令，否则生成 `(高电平通道列表, 时长)` 指令；相同高电平通道集合的相邻区间会合并。
- `LoopCount ≥ 1000000` 时拆成嵌套循环（外层 `LoopCount/1000000`，内层 1000000，余数再包一层）。

---

## 5. 序列组合文件 `SequenceGroup\{组合名}.userdat`

- 描述区固定 1 键：`GroupName★{组合名}`（与文件名一致）。
- 数据区结构与第 4 节完全相同（`ChannelNames` + 每通道 4 列），但：
  - 通道名是**自由字符串**（不要求 `Ch_x` 枚举名），且不可重复；
  - 所有峰都是普通峰（无嵌套组合），`PeakValues` 都是 `Zero`/`One`，`IsTrigger` 都是 bool。
- 被序列引用时：`SelectChnnelInd`（序列侧 PeakValues 列）指定展开用组内哪条通道的波形；组合峰时长 = 该通道峰长总和。

---

## 6. 实验结果文件（核心）

**保存路径**：
```
{保存根目录}\{ODMRExperimentGroupName}\{ODMRExperimentName}\{ODMRExperimentName}{yyyy_MM_dd_HH_mm_ss}.userdat
```
- 保存根目录 = 实验页面 `SavePath` 用户选择的目录（不是运行目录）。
- 实验结束自动保存（`IsAutoSave=true` 时）或手动保存；时间戳为保存时刻。

**描述区**（键内 `→` 为逻辑分层）：

| 键 | 值 | 说明 |
|---|---|---|
| `实验类型` | 枚举名：`磁场调节`/`源表IV测量数据`/`自定义数据`/`温度监测数据`/`ODMR实验` | 文件类型判别键 |
| `开始时间` | **OADate double**（如 `46259.4375`，1899-12-30 起的天数） | 读取逻辑：含 `:` 按 DateTime 字符串解析，否则 `DateTime.FromOADate` |
| `结束时间` | OADate double | 同上 |
| `实验分组` | 分组名 | |
| `实验名` | 实验名 | |
| `Input→{参数描述}→{属性名}` | 参数值字符串 | 每个输入参数一条 |
| `Output→{参数描述}→{属性名}` | 参数值字符串 | 每个输出参数一条（含实验算出的结果值） |
| `Dev→{设备描述}→{属性名}→{DeviceTypes枚举名}` | 设备值字符串 | 每个设备一条 |

参数值序列化规则（`GetUnknownParamValueToString`）：double/int/bool → `ToString()`；枚举 → 枚举名；DateTime → `ToLongTimeString()`；**其他类型（如数组）→ 空字符串**。

**数据区**（图表数据）：

| 数据集名格式 | 类型 | 内容 |
|---|---|---|
| `C1DData→{曲线名}→{组名}→{X\|Y\|XY\|None}` | 单列 double | 一维曲线数据。ODMR 实验按"一条曲线 = X 数据集 + Y 数据集"拆分保存：轴类型 `X` 的存横坐标值，`Y` 的存纵坐标值，**靠组名（第 2 段）配对**（一组内 1 个 X、可多个 Y）。`XY`/`None` 主要出现在自定义数据模块。 |
| `C2DData→{组名}→{XName}→{YName}→{ZName}→{XLo}→{XHi}→{XCounts}→{YLo}→{YHi}→{YCounts}` | 点数据（☯ 分隔） | 二维热力图。行 = Y 方向（共 YCounts 行），每行 = 该行沿 X 方向的 XCounts 个 Z 值用 `☯` 连接。即 `第i行第j个值 = Z[j, i]`（x 索引在前）。坐标轴为连续范围 `[XLo,XHi]` × `[YLo,YHi]`，热力图把 Z 矩阵铺满该范围（重建坐标时按均匀网格处理）。缺行/缺值用 `❤`。 |

注意：
- 拟合数据（`D1FitDatas`）**不写入**结果文件。
- C1D 同一组名的 X 数据集与 Y 数据集长度应当一致；解析成曲线时按"同组名 → 横轴取 X 数据集、每条 Y 数据集一条曲线"还原。
- 数据集名中的曲线名可能本身含 `:` 或空格（如 `距离(nm)`、`AFM形貌数据(PID输出)`），但**不含 `★❤☯■●`**（写入端有校验）。

---

## 7. 实验参数配置文件 `ODMRConfig\{分组}_{实验名}.userdat`

- 无数据区，纯描述区。每个实验一个文件，启动时由 ParamManager 批量恢复。
- 键格式（注意与结果文件的 `→` 段顺序**不同**：这里是 实验名 在前、分组 在后）：
  ```
  Input→{参数描述}→{属性名}→{实验名}→{分组名}→{完整类型名}★{值}
  Dev→{设备描述}→{属性名}→{实验名}→{分组名}→{完整类型名}★{值}
  IsAutoSave→{实验名}→{分组名}→{完整类型名}★True/False
  Reverse2DX→{实验名}→{分组名}→{完整类型名}★True/False
  Reverse2DY→{实验名}→{分组名}→{完整类型名}★True/False
  ```
- 完整类型名示例：`ODMR_Lab.实验部分.ODMR实验.实验方法.AF M.AFMScanDistanceExp`（用于区分同名实验的不同实现类）。
- 值序列化规则同第 6 节。

---

## 8. 数据记录（笔记）系统（扩展部分，可选支持）

目录结构（在用户选择的记录根目录下）：
```
note{N}\                              ← 一条笔记（N 为序号）
  data.userdat                        ← 描述区: Name★{笔记名} + 内部标签数据集
  globaltagdata.userdat               ← 全局标签
  parenttagdata.userdat               ← 父级标签
  {yyyy-MM-dd-HH-mm-ss}\              ← 一个条目（按创建时间命名）
    data.userdat                      ← 描述区: Description★{描述}, CreateTime★{OADate} + 内部标签
    parenttagdata.userdat             ← 父级标签
    *.png / *.jpg / *.pdf             ← 附件（实验截图等，非 userdat）
```
标签数据集名格式（`♠` U+2660 分层）：
- `PureTextTag♠{Color}`（值 = 标签文本，1 行）
- `CaptionTextTag♠{Color}♠{标题}`（值 = 内容）
- `SingleOptionTag♠{Color}♠{选中项索引}♠{标题}`（值 = 全部选项，多行）
- `MultiOptionTag♠{Color}♠{标题}♠{索引1♠索引2♠...}`（值 = 全部选项）
- `{Color}` 为 WPF Color 字符串（`#AARRGGBB`）。

---

## 9. 自定义数据文件（自定义数据模块）

任意命名的 `.userdat`：
- 数据集名格式：`{名}` 或 `{名}${组名}${轴类型}`（`$` 分隔，轴类型 ∈ X/Y/XY/None）。
- 数据类型由 `JudgeDataType` 推断（double / DateTime / string / 点数据），DateTime 用 `yyyy:MMMM:dd:HH:mm:ss:FFF`。
- 描述区可带任意元数据键值。

---

## 10. 给解析 Skill 的建议

**推荐实现顺序**：
1. **通用 FileObject 解析器**（第 2 节）——所有文件的底座。输出：`{descriptions: dict, data: {列名: [元素...]}}`，元素保留原始字符串 + 类型推断结果；处理 `❤`（None）、`☯`（点）、`■/●`（换行还原）。
2. **文件类型识别**（看描述区首键，优先级）：
   - 有 `实验类型` → 实验结果文件（第 6 节）
   - 有 `SequenceAssembleName` → 序列（第 4 节）
   - 有 `GroupName` → 序列组合（第 5 节）
   - 键全为 `{名}→{bool}` 且位于 `Sequences\GlobalPulses.userdat` → 全局脉冲表（第 3 节）
   - 有 `Name`+`CreateTime` → 笔记条目；有 `Name`+标签数据集 → 笔记（第 8 节）
   - 数据集名含 `$` → 自定义数据（第 9 节）
   - 其余 → 通用键值文件（ODMRConfig/UIParam 等，第 7 节）
3. **实验结果 → 结构化数据**：
   - 时间：OADate double ↔ DateTime（`datetime(1899,12,30) + timedelta(days=v)`）。
   - C1D：按 `组名` 聚合，X 数据集做横轴，各 Y 数据集成曲线；输出 `{组: {x: [], 曲线名: []}}`。
   - C2D：解析 11 段键名 + 点数据 → `{XName, YName, ZName, XLo, XHi, XCounts, YLo, YHi, YCounts, Z: [YCounts][XCounts]}`；坐标网格按均匀分布重建。
   - 参数：`Input→...`/`Output→...`/`Dev→...` 按 `→` 切分还原成 `[{类型, 描述, 属性名, (设备类型), 值}]`。
4. **序列 → 执行时序**（如 Skill 需要）：
   - 先读 `GlobalPulses.userdat`，覆盖命中脉冲名的 Spans；
   - 组合峰按 `SequenceGroup` 文件递归展开（注意 PeakValues=组内通道索引）；
   - `❤` 行还原为"短通道提前结束"；
   - 输出每通道 `[ (起始ns, 结束ns, 电平0/1, 是否Trigger, 峰名) ]`，并做第 4 节的 4 条一致性校验。

**必须注意的坑**（汇总）：
1. 时间单位是 **ns（整数）**；结果文件时间是 **OADate double**；笔记/自定义数据时间是 `yyyy:MMMM:dd:...`（月份名依赖区域语言）。
2. 序列 Spans 只是初始值，**真实运行长度以 GlobalPulses 表为准**（仅当峰名命中时）。
3. 值中含 `★` 会被截断；键重复会导致整个文件读不了（解析器遇到重复键应报清晰错误而不是静默覆盖）。
4. 描述行 value 可为空；数据行列数必须恒定。
5. double 特殊字面量：`NaN`、`非数字`、`正无穷大`、`负无穷大`。
6. 结果文件保存在**用户目录**而非程序目录；程序目录内只有 Sequences/SequenceGroup/ODMRConfig/UIParam 等。
7. 所有文本按 UTF-8 读（含中文键名、Unicode 限定符），不要按 ANSI。

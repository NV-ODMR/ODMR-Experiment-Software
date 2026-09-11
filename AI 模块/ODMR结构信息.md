# ODMR 结构信息

## 1. 项目路径
- 项目根目录：D:\ODMR\ODMR
- 源码目录：D:\ODMR\ODMR\AI 模块\
- 运行目录：D:\ODMR\ODMR\bin\x64\Debug\AI 模块\
- 知识库文件：ODMR结构信息.md（源码目录与运行目录各一份，需同步更新）

## 2. 程序架构
- 主程序：ODMR-Lab.exe（WPF 应用）
- AI 服务：AIService.cs（HTTP 监听，端口 8080）
- 实验控制：AIService.Experiments.cs（实验管理指令）
- 设备控制：AIService.Devices.cs（设备操作指令）
- 数据导出：AIService.Data.cs（数据导出指令）

## 3. 指令分类
共 39 条指令，分为：
- 实验管理（12条）：select-exp, get-exp-params, set-exp-param, start-experiment, stop-experiment, exp-status, list-experiments, get-exp-outputs, export-data, wait-experiment 等
- 设备控制（15条）：device-list, device-get, device-set, laser-on, laser-off, laser-get-power, laser-set-power 等
- 页面操作（5条）：switch-page, click-button, get-button-state, get-textbox-value, set-textbox-value
- 系统指令（7条）：help, ping, get-logs, read-exp-source, read-odmr-memory, update-odmr-memory 等

## 4. 实验流程
标准流程：
1. list-experiments 列出所有实验
2. select-exp index=N 选择实验
3. get-exp-params 获取参数列表
4. set-exp-param 设置参数（可多次）
5. start-experiment 启动实验
6. wait-experiment 阻塞等待实验完成（推荐）或 exp-status 轮询
7. get-exp-outputs 获取输出文件列表
8. export-data file=xxx 导出数据

## 5. 实验类型
- 点实验：在界面上选择具体实验项（如 CW全谱、Rabi 等）
- 参数实验：通过参数配置运行
- 实验结果存储在 .userdat 文件中

## 6. 轮询规则（重要）
- **优先使用 wait-experiment**：阻塞等待实验完成
- **短超时+自动重试策略**：
  - 调用 wait-experiment 时设置较短的 timeout（如 30 秒）
  - 如果返回"已取消一个任务"（超时），不要停下来等用户回复
  - 立即再次调用 wait-experiment 继续等待
  - 循环直到实验完成（running=false）
  - 这样既避免 HTTP 长时间阻塞超时，又不会中断等待流程
- 参数：timeout=秒（推荐30）、interval=毫秒（默认1000）
- 返回：running、progress、state、dataFile 等
- 仅在用户主动询问进度时才用 exp-status
- **禁止无意义轮询**：不要每隔几秒就查一次状态

## 7. 安全模式
- safe 模式：所有指令可直接执行
- dangerous 模式：写入类指令需 confirm=true
- 位移台移动永久禁止 AI 执行

## 8. 数据文件格式
.userdat 文件结构：
- 描述区：`键★值` 格式
- 分隔符：`userdata description ending line`
- 数据区：`data name line★数据集名`
- 数据行：★ 分隔列，❤ 表示缺值
- 结束符：`end of file`

## 9. 实验类型判断
- `实验类型★SequenceAssembleName` → 脉冲序列
- `实验类型★GroupName` → 组合
- `实验类型★C1DData` → 一维曲线结果
- `实验类型★C2DData` → 二维热力图结果

## 10. 常用实验索引（示例）
- CW全谱：index 6
- Rabi：index 42
- 其他实验需通过 list-experiments 查询

## 11. 线程模型
- AIService.ListenLoop 使用 ThreadPool.QueueUserWorkItem 分发请求
- 每个请求在独立线程池线程运行
- Thread.Sleep 只阻塞当前请求线程，不影响 UI / 实验 / 其他指令

## 12. 记忆库更新规则
- 修改后必须同时更新源码目录和运行目录的两份文件
- 使用 update-odmr-memory 指令，需 confirm=true
- 更新后验证两份文件内容一致

## 13. wait-experiment 指令
- 位置：AIService.Experiments.cs，exp-status 之后、stop-experiment 之前
- 功能：阻塞等待当前实验完成
- 参数：
  - timeout=秒（默认600，最大3600，推荐30用于短超时重试）
  - interval=毫秒（默认1000，最小100）
- 返回值：
  - running: false（实验已结束）
  - progress: 最终进度
  - state: 实验状态（Completed/Error/Stopped）
  - error: 错误信息（如有）
  - dataFile: 输出文件路径（如有）
  - waitSeconds: 实际等待秒数
- 线程安全：使用 Thread.Sleep 阻塞当前线程池线程，其他请求正常处理
- 使用场景：start-experiment 后立即调用，使用短超时+自动重试策略

## 14. 图片保存路径配置
- **配置路径**：C:\Users\USER\Desktop\Pictures
- **首次使用规则**：
  - 当用户第一次使用绘图功能时，检查记忆库中是否有本节配置
  - 如果没有配置，询问用户希望把生成的图片存放在哪里
  - 验证用户提供的路径不包含中文字符（使用 Python 检查 '\u4e00' <= char <= '\u9fff'）
  - 验证通过后，更新记忆库本节内容，将路径写入配置
  - 之后所有绘图操作都使用该路径保存
- **绘图脚本规范**：
  - 从记忆库读取图片保存路径
  - 使用 os.makedirs(path, exist_ok=True) 确保目录存在
  - 保存图片时指定完整路径，如：plt.savefig(os.path.join(IMAGE_SAVE_PATH, 'filename.png'))
  - 在回答中使用 Markdown 图片语法展示：![图片说明](完整路径)
- **路径验证**：路径中不能包含中文字符，避免某些程序读取时出错
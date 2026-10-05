using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Label = System.Windows.Controls.Label;

namespace ODMR_Lab
{
    /// <summary>
    /// 实验停止异常
    /// </summary>
    public class ExpStopException : Exception
    {
        public override string Message { get; } = "实验被中止";
    }

    /// <summary>
    /// 实验基类型
    /// </summary>
    /// <typeparam name="ParamType"></typeparam>
    /// <typeparam name="ConfigType"></typeparam>
    public abstract class ExperimentObject<ParamType, ConfigType>
        where ParamType : ExpParamBase
        where ConfigType : ConfigBase
    {

        public ExperimentObject()
        {
            JudgeThreadEndOrResumeAction = JudgeThreadEndOrResume;
        }

        /// <summary>
        /// 非法字符
        /// </summary>
        public static List<char> InvaliidChars = new List<char>() { '$' };

        /// <summary>
        /// ★ 多实验并行改造（2026-09-20）：由 static 改为【实例字段】。
        /// <para>原因：static 字段被所有实验对象共享，实验A结束时 SetStopState() 会把它清零，
        /// 导致并行运行的实验B丢失 AI 标志 → 异常时弹窗阻塞 UI 线程；
        /// AIService 的 finally 也会把 B 正在用的标志提前清掉。</para>
        /// <para>改为实例字段后各实验对象互相隔离，48 处实例方法内的读取点自动解析为 this.SkipPreConfirm，
        /// 无需改动。</para>
        /// AI 启动本实验时设为 true，跳过 PreConfirmProcedure 弹框。
        /// </summary>
        public volatile bool SkipPreConfirm = false;

        /// <summary>
        /// SkipPreConfirm 设置时间，用于超时自动重置（实例字段，随实验对象隔离）
        /// </summary>
        private DateTime skipPreConfirmSetTime = DateTime.MinValue;

        /// <summary>
        /// ★ 多实验并行改造（2026-09-20）：由 static 改为【实例字段】。
        /// AI 控制本实验期间为 true，抑制实验启动/运行/异常流程中的弹窗（ShowTipWindow / ShowMessageBox）。
        /// 改为实例字段后，实验A结束（SetStopState）不再污染并行运行的实验B。
        /// 用户直接操作时此标志始终为 false，所有弹窗行为不受影响。
        /// </summary>
        public volatile bool AIControlled = false;

        /// <summary>
        /// 设置 SkipPreConfirm 标志（AI 专用）。实例方法，仅作用于本实验对象。
        /// </summary>
        public void SetSkipPreConfirm(bool value)
        {
            SkipPreConfirm = value;
            if (value)
            {
                skipPreConfirmSetTime = DateTime.Now;
            }
        }

        /// <summary>
        /// ★ 多实验并行新增：一次性设置【本实验对象】的 AI 控制标志。
        /// 替代原先 AIService 通过反射写静态字段的写法（静态字段无法区分并行实验）。
        /// </summary>
        public void SetAIControl(bool value)
        {
            AIControlled = value;
            SkipPreConfirm = value;
            if (value) skipPreConfirmSetTime = DateTime.Now;
        }

        /// <summary>
        /// 实验文件类型
        /// </summary>
        public abstract ExperimentFileTypes ExpType { get; protected set; }

        /// <summary>
        /// 实验开始时间
        /// </summary>
        public DateTime ExpStartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 实验结束时间
        /// </summary>
        public DateTime ExpEndTime { get; set; } = DateTime.Now;

        public void SetStartTime(DateTime time)
        {
            ExpStartTime = time;
        }

        public void SetEndTime(DateTime time)
        {
            ExpEndTime = time;
        }

        /// <summary>
        /// 实验参数
        /// </summary>
        public abstract ParamType Param { get; set; }

        /// <summary>
        /// UI显示参数
        /// </summary>
        public abstract ConfigType Config { get; set; }

        public bool ReadFromFile(string filepath)
        {
            FileObject obj = FileObject.ReadFromFile(filepath);
            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                throw new Exception("此文件不属于实验类型文件");
            }

            if (ExpType == (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), obj.Descriptions["实验类型"]))
            {
                //读取实验时间
                if (obj.Descriptions["开始时间"].Contains(":") && obj.Descriptions["结束时间"].Contains(":"))
                {
                    try
                    {
                        ExpStartTime = DateTime.Parse(obj.Descriptions["开始时间"]);
                        ExpEndTime = DateTime.Parse(obj.Descriptions["结束时间"]);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    try
                    {
                        ExpStartTime = DateTime.FromOADate(double.Parse(obj.Descriptions["开始时间"]));
                        ExpEndTime = DateTime.FromOADate(double.Parse(obj.Descriptions["结束时间"]));
                    }
                    catch (Exception)
                    {

                    }
                }

                InnerRead(obj);

                if (Param != null)
                {
                    Param.ReadFromDescription(obj.Descriptions);
                }
                if (Config != null)
                {
                    Config.ReadFromDescription(obj.Descriptions);
                }

                return true;
            }
            return false;
        }

        public bool ReadFromExplorer(out string filename)
        {
            filename = "";
            FileObject obj = FileObject.FindFileFromExplorer();
            filename = obj.FilePath;
            if (obj == null) return false;
            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                throw new Exception("此文件不属于实验类型文件");
            }

            if (ExpType == (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), obj.Descriptions["实验类型"]))
            {
                //读取实验时间
                if (obj.Descriptions["开始时间"].Contains(":") && obj.Descriptions["结束时间"].Contains(":"))
                {
                    try
                    {
                        ExpStartTime = DateTime.Parse(obj.Descriptions["开始时间"]);
                        ExpEndTime = DateTime.Parse(obj.Descriptions["结束时间"]);
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    try
                    {
                        ExpStartTime = DateTime.FromOADate(double.Parse(obj.Descriptions["开始时间"]));
                        ExpEndTime = DateTime.FromOADate(double.Parse(obj.Descriptions["结束时间"]));
                    }
                    catch (Exception)
                    {

                    }
                }

                InnerRead(obj);

                if (Param != null)
                {
                    Param.ReadFromDescription(obj.Descriptions);
                }

                if (Config != null)
                {
                    Config.ReadFromDescription(obj.Descriptions);
                }

                return true;
            }
            return false;
        }

        public void WriteToFile(string filefolder, string filename)
        {
            if (!Directory.Exists(filefolder))
            {
                Directory.CreateDirectory(filefolder);
            }

            FileObject obj = new FileObject();

            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
            }
            if (!obj.Descriptions.Keys.Contains("开始时间"))
            {
                obj.Descriptions.Add("开始时间", ExpStartTime.ToOADate().ToString());
            }
            if (!obj.Descriptions.Keys.Contains("结束时间"))
            {
                obj.Descriptions.Add("结束时间", ExpEndTime.ToOADate().ToString());
            }

            InnerWrite(obj);

            if (Param != null)
            {

                Dictionary<string, string> dic = Param.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }
            if (Config != null)
            {

                Dictionary<string, string> dic = Config.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }

            obj.SaveToFile(Path.Combine(filefolder, filename));
        }

        public bool WriteFromExplorer(string defaultName = "")
        {
            FileObject obj = new FileObject();

            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
            }
            if (!obj.Descriptions.Keys.Contains("开始时间"))
            {
                obj.Descriptions.Add("开始时间", ExpStartTime.ToOADate().ToString());
            }
            if (!obj.Descriptions.Keys.Contains("结束时间"))
            {
                obj.Descriptions.Add("结束时间", ExpEndTime.ToOADate().ToString());
            }

            InnerWrite(obj);

            if (Param != null)
            {
                Dictionary<string, string> dic = Param.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }

            if (Config != null)
            {
                Dictionary<string, string> dic = Config.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }

            return obj.SaveFileFromExplorer(defaultname: defaultName);
        }

        /// <summary>
        /// 获取文件的实验类型
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static ExperimentFileTypes GetExpType(string filepath)
        {
            Dictionary<string, string> dic = FileObject.ReadDescription(filepath);
            if (!dic.Keys.Contains("实验类型"))
            {
                return ExperimentFileTypes.None;
            }
            try
            {
                return (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), dic["实验类型"]);
            }
            catch (Exception ex)
            {
                return ExperimentFileTypes.None;
            }
        }

        public static void GetExpTime(string filepath, out DateTime starttime, out DateTime endtime)
        {
            Dictionary<string, string> dic = FileObject.ReadDescription(filepath);

            if (dic["开始时间"] == "" || dic["结束时间"] == "")
            {
                throw new Exception("未能正确读取实验时间");
            }

            //读取实验时间
            if (dic["开始时间"].Contains(":") && dic["结束时间"].Contains(":"))
            {
                try
                {
                    starttime = DateTime.Parse(dic["开始时间"]);
                    endtime = DateTime.Parse(dic["结束时间"]);
                    return;
                }
                catch (Exception)
                {
                }
            }
            else
            {
                try
                {
                    starttime = DateTime.FromOADate(double.Parse(dic["开始时间"]));
                    endtime = DateTime.FromOADate(double.Parse(dic["结束时间"]));
                    return;
                }
                catch (Exception)
                {

                }
            }

            throw new Exception("未能正确读取实验时间");
        }

        /// <summary>
        /// 内部读操作，将obj中的信息转化成对应的ExperimentFileObject
        /// </summary>
        /// <param name="fobj"></param>
        protected abstract void InnerRead(FileObject fobj);

        /// <summary>
        /// 内部写操作，将ExperimentFileObject中的信息转化成FileObject
        /// </summary>
        /// <returns></returns>
        protected abstract void InnerWrite(FileObject obj);

        public DataVisualSource ToDataVisualSource()
        {
            DataVisualSource s = new DataVisualSource();

            s.Params.Add("实验类型", Enum.GetName(ExpType.GetType(), ExpType));
            s.Params.Add("开始时间", ExpStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            s.Params.Add("结束时间", ExpEndTime.ToString("yyyy-MM-dd HH:mm:ss"));

            InnerToDataVisualSource(s);
            return s;
        }
        /// <summary>
        /// 转换成数据可视化对象
        /// </summary>
        /// <returns></returns>
        protected abstract void InnerToDataVisualSource(DataVisualSource source);

        #region 实验线程部分
        Thread ExpThread { get; set; } = null;

        /// <summary>
        /// 线程运行时的相关控件显示状态
        /// </summary>
        private List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();

        #region 实验通信控件

        DecoratedButton startButton = null;

        DecoratedButton resumeButton = null;

        DecoratedButton stopButton = null;


        string CurrentexpState = "";
        /// <summary>
        /// 实验进度标签
        /// </summary>
        private TextBlock CurrentexpStateTextBlock = null;

        double CurrentProgress = 0;

        ProgressBar CurrentProgressBar = null;

        private Label ExpStartTimeLabel { get; set; } = null;
        private Label ExpEndTimeLabel { get; set; } = null;

        /// <summary>
        /// ★ 多实验并行：本实验运行期间因用户切到别的实验标签而被"解绑显示"。
        /// 为 true 时不再向已转交的控件写状态（避免污染当前显示实验的界面）。
        /// </summary>
        private volatile bool _uiDetached = false;

        /// <param name="takeDisplayOwnership">
        /// ★ 多实验并行（2026-09-20）：true 表示本实验【重新获得显示权】，需强制解绑旧控件后重挂，
        /// 防止事件重复挂载（如两次 Click += StartEvent）。
        /// false（默认，兼容原有 8 种调用方式）= 不解绑，直接重挂 —— 但若之前调用过
        /// DetachForOtherExperiment，也会一并清掉残留引用。
        /// </param>
        public void ConnectOuterControl(DecoratedButton StartBtn, DecoratedButton StopBtn, DecoratedButton ResumeBtn, Label StartTimeLabel, Label EndTimeLabel, TextBlock ThreadState, ProgressBar ThreadProgress, List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlPanels, bool takeDisplayOwnership = false)
        {
            // ★ 多实验并行：重新获得显示权时强制清掉旧绑定（防止事件重复挂载）后再挂新控件
            DisConnectOuterControl(takeDisplayOwnership);
            _uiDetached = false;
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (StartBtn != null)
                {
                    startButton = StartBtn;
                    startButton.Click -= StartEvent;
                    startButton.Click += StartEvent;
                }
                if (StopBtn != null)
                {
                    stopButton = StopBtn;
                    stopButton.Click -= StopEvent;
                    stopButton.Click += StopEvent;
                }
                if (ResumeBtn != null)
                {
                    resumeButton = ResumeBtn;
                    resumeButton.Click -= ResumeEvent;
                    resumeButton.Click += ResumeEvent;
                }

                ExpStartTimeLabel = StartTimeLabel;
                if (ExpStartTimeLabel != null)
                {
                    ExpStartTimeLabel.Content = ExpStartTime;
                }
                ExpEndTimeLabel = EndTimeLabel;
                if (ExpEndTimeLabel != null)
                {
                    ExpEndTimeLabel.Content = ExpEndTime;
                }

                CurrentexpStateTextBlock = ThreadState;
                if (CurrentexpStateTextBlock != null)
                {
                    CurrentexpStateTextBlock.Text = CurrentexpState;
                }

                CurrentProgressBar = ThreadProgress;
                if (CurrentProgressBar != null)
                {
                    CurrentProgressBar.Value = CurrentProgress;
                }

                ControlStates = ControlPanels;
                //根据此实验刷新面板状态
                if (IsExpEnd)
                {
                    SetPanelStopState();
                    return;
                }
                if (IsExpResume)
                {
                    SetPanelResumeState();
                    return;
                }
                SetPanelStartState();
            });
        }

        /// <summary>
        /// 解绑与外部控件的连接（默认：运行中的实验【不】解绑，见下）
        /// </summary>
        public void DisConnectOuterControl()
        {
            DisConnectOuterControl(false);
        }

        /// <summary>
        /// 解绑与外部控件的连接。
        /// <para>★ 多实验并行改造（2026-09-20）：新增 force 参数。
        /// 原实现无条件清空全部控件引用，导致"实验A运行中，用户切到实验B标签"时
        /// A 的控件引用被置 null —— 但 A 的 ExpThread 仍在调用 SetExpState/SetProgress/
        /// SetPanelStopState，进而去操作【B 的按钮与进度条】，造成界面状态错乱。</para>
        /// <para>现在：实验仍在运行（ExpThread 存活）时只标记 _uiDetached（停止写 UI，保留引用），
        /// 等实验结束自然释放；force=true 用于重新绑定和实验结束后的强制清理。</para>
        /// </summary>
        public void DisConnectOuterControl(bool force)
        {
            // 运行中的实验保留控件引用，仅切换"不再写 UI"的标记
            if (!force && !IsExpEnd && ExpThread != null && ExpThread.IsAlive)
            {
                _uiDetached = true;
                return;
            }
            _uiDetached = false;
            if (startButton != null)
            {
                startButton.Click -= StartEvent;
                startButton = null;
            }
            if (resumeButton != null)
            {
                resumeButton.Click -= ResumeEvent;
                resumeButton = null;
            }
            if (stopButton != null)
            {
                stopButton.Click -= StopEvent;
                stopButton = null;
            }
            if (ExpStartTimeLabel != null)
                ExpStartTimeLabel = null;
            if (ExpEndTimeLabel != null)
                ExpEndTimeLabel = null;
            if (CurrentexpStateTextBlock != null)
                CurrentexpStateTextBlock = null;
            if (CurrentProgressBar != null)
                CurrentProgressBar = null;
            ControlStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();
        }

        /// <summary>
        /// ★ 多实验并行新增（2026-09-20）：把"界面显示权"转交给另一个实验时调用。
        /// <para>为什么不能直接用 DisConnectOuterControl：运行中的实验需要保留进度条等引用
        /// 以便切回标签页时恢复显示，但【按钮必须解绑】—— 因为按钮池（StartBtn/StopBtn/ResumeBtn）
        /// 是页面共享的，若运行中的实验 A 不解绑，另一个实验 B 再挂一次，用户点一次"开始"
        /// 会同时触发 A 和 B 的 StartEvent，造成严重误操作。</para>
        /// <para>解绑按钮后 _uiDetached=true，A 的 ExpThread 仍正常跑，但不再写共享 UI。</para>
        /// </summary>
        public void DetachForOtherExperiment()
        {
            // 实验已结束：直接彻底解绑即可
            if (IsExpEnd || ExpThread == null || !ExpThread.IsAlive)
            {
                DisConnectOuterControl(true);
                return;
            }
            // 运行中：标记不再写 UI，保留进度/状态引用，但必须解绑共享按钮
            _uiDetached = true;
            if (startButton != null)
            {
                startButton.Click -= StartEvent;
                startButton = null;
            }
            if (stopButton != null)
            {
                stopButton.Click -= StopEvent;
                stopButton = null;
            }
            if (resumeButton != null)
            {
                resumeButton.Click -= ResumeEvent;
                resumeButton = null;
            }
        }

        public void DisConnectODMRParentExperiment()
        {
            JudgeThreadEndOrResumeAction = JudgeThreadEndOrResume;
        }

        public void ConnectODMRParentExperiment(ODMRExpObject obj)
        {
            JudgeThreadEndOrResumeAction = obj.JudgeThreadEndOrResume;
        }

        /// <summary>
        /// 设置当前线程状态
        /// </summary>
        /// <param name="state"></param>
        public void SetExpState(string state)
        {
            CurrentexpState = state;
            // ★ 多实验并行：控件已转交给其他实验显示时只更新内存状态，不写 UI
            if (_uiDetached) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentexpStateTextBlock != null)
                {
                    CurrentexpStateTextBlock.Text = state;
                    CurrentexpStateTextBlock.ToolTip = state;
                }
            });
        }

        /// <summary>
        /// 设置当前线程状态
        /// </summary>
        /// <param name="state"></param>
        public string GetExpState()
        {
            return CurrentexpState;
        }

        /// <summary>
        /// 设置进度(0-100)
        /// </summary>
        public void SetProgress(double value)
        {
            CurrentProgress = value;
            // ★ 多实验并行：控件已转交给其他实验显示时只更新内存进度，不写 UI
            if (_uiDetached) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentProgressBar != null)
                {
                    CurrentProgressBar.Value = value;
                }
            });
        }

        /// <summary>
        /// 获取当前进度(0-100)，供外部(如 AI 服务)查询
        /// </summary>
        public double GetProgress()
        {
            return CurrentProgress;
        }
        #endregion

        /// <summary>
        /// 初始化事件，在读取参数和获取设备后触发
        /// </summary>
        public event Action InitEvent = null;

        public event Action ResumeStateEvent = null;
        public event Action EndStateEvent = null;
        public event Action ErrorStateEvent = null;

        private void SetStartState()
        {
            SetPanelStartState();
        }

        public void SetPanelStartState()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(startButton, false);
                TrySetState(stopButton, true);
                TrySetState(resumeButton, true);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning)
                        item.Key.IsHitTestVisible = true;
                    if (item.Value == RunningBehaviours.DisableWhenRunning || item.Value == RunningBehaviours.DisableWhenRunningEnableWhenResume)
                        item.Key.IsHitTestVisible = false;
                }
            });
            IsExpEnd = false;
            IsExpResume = false;
        }

        private void SetResumeState()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                SetPanelResumeState();
                ResumeStateEvent?.Invoke();
            });
            IsExpResume = true;
            IsExpEnd = false;
        }

        public void SetPanelResumeState()
        {
            // ★ 多实验并行：显示权已转交其他实验时不碰共享控件
            if (_uiDetached) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(startButton, true);
                TrySetState(stopButton, true);
                TrySetState(resumeButton, false);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning || item.Value == RunningBehaviours.DisableWhenRunningEnableWhenResume)
                        item.Key.IsHitTestVisible = true;
                    if (item.Value == RunningBehaviours.DisableWhenRunning)
                        item.Key.IsHitTestVisible = false;
                }
            });
        }

        private void SetStopState()
        {
            // 实验结束时自动重置 AI 控制标志，确保下次用户操作时弹窗正常显示
            AIControlled = false;
            App.Current.Dispatcher.Invoke(() =>
            {
                SetPanelStopState();
                EndStateEvent?.Invoke();
            });
            IsExpEnd = true;
            IsExpResume = false;
        }

        public void SetPanelStopState()
        {
            // ★ 多实验并行：显示权已转交其他实验时不碰共享控件（避免把别人正在运行的
            //   实验的「开始」按钮重新启用，导致误触重启）。
            //   切回本实验标签时 ConnectOuterControl 会重新应用停止态。
            if (_uiDetached) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(startButton, true);
                TrySetState(stopButton, false);
                TrySetState(resumeButton, false);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning)
                        item.Key.IsHitTestVisible = false;
                    if (item.Value == RunningBehaviours.DisableWhenRunning || item.Value == RunningBehaviours.DisableWhenRunningEnableWhenResume)
                        item.Key.IsHitTestVisible = true;
                }
            });
        }

        private void TrySetState(FrameworkElement ele, bool state)
        {
            if (ele == null) return;
            else
            {
                if (ele is DecoratedButton)
                {
                    if (state) (ele as DecoratedButton).KeepPressed = false;
                    else
                    {
                        (ele as DecoratedButton).KeepPressed = true;
                    }
                }
                ele.IsEnabled = state;
            }
        }

        public bool IsExpEnd { get; set; } = true;
        public bool IsExpResume { get; set; } = false;

        #region 子实验参数
        /// <summary>
        /// 是否是子实验
        /// </summary>
        public bool IsSubExperiment { get; set; } = false;

        private Exception expFailedException = null;
        public Exception ExpFailedException { get { return expFailedException; } }
        #endregion

        private void StartEvent(object sender, RoutedEventArgs e)
        {
            expFailedException = null;
            if (ThreadResumeFlag == true && ThreadEndFlag == false)
            {
                ThreadResumeFlag = false;
                SetStartState();
                return;
            }
            SetStartState();
            bool IsContinue = true;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (!IsSubExperiment)
                {
                    try
                    {
                        // 始终调用 PreConfirmProcedure，让各实验内部自行判断 SkipPreConfirm 来跳过弹窗
                        // 这样既保留了副作用操作（如 GetDevices、DropConfirm 等），又避免了弹窗阻塞
                        IsContinue = PreConfirmProcedure();
                    }
                    catch (Exception ex)
                    {
                        IsContinue = false;
                        if (!AIControlled) MessageWindow.ShowTipWindow("实验未成功进行:\n" + ex.Message, MainWindow.Handle);
                    }
                }
            });
            if (!IsContinue)
            {
                //设值结束状态
                SetStopState();
                return;
            }
            ThreadEndFlag = false;
            ThreadResumeFlag = false;
            IsExpEnd = false;
            IsExpResume = false;
            ExpThread = new Thread(() =>
            {
                List<InfoBase> Devices = new List<InfoBase>();
                try
                {
                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            tempConfig = ReadConfig();
                        });
                        //读取参数
                    }
                    catch (Exception ex)
                    {
                        if (!AIControlled) MessageWindow.ShowTipWindow("参数设置存在错误:\n" + ex.Message, MainWindow.Handle);
                        ErrorStateEvent?.Invoke();
                        SetStopState();
                        return;
                    }

                    try
                    {
                        //如果是子实验则不占用设备
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Devices = GetDevices();
                            DeviceDispatcher.UseDevices(Devices);
                        });
                    }
                    catch (Exception ex)
                    {
                        if (!AIControlled) MessageWindow.ShowTipWindow("设备获取失败:\n" + ex.Message, MainWindow.Handle);
                        ErrorStateEvent?.Invoke();
                        SetStopState();
                        return;
                    }

                    InitEvent?.Invoke();

                    //设置实验时间
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SetStartTime(DateTime.Now);
                        if (ExpStartTimeLabel != null)
                        {
                            ExpStartTimeLabel.Content = ExpStartTime;
                        }
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = "";
                        }
                    });
                    ThreadEndFlag = false;
                    ThreadResumeFlag = false;
                    Config = tempConfig;
                    //进行试验
                    ExperimentEvent();
                    //设值结束状态
                    SetStopState();
                    //结束占用设备
                    DeviceDispatcher.EndUseDevices(Devices);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SetEndTime(DateTime.Now);
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = ExpEndTime;
                        }
                    });
                }
                catch (Exception ex)
                {
                    DeviceDispatcher.EndUseDevices(Devices);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SetEndTime(DateTime.Now);
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = ExpEndTime;
                        }
                    });
                    if (ex is ExpStopException)
                    {
                        if (!IsSubExperiment)
                        {
                            if (!AIControlled) MessageWindow.ShowTipWindow("实验已被停止", MainWindow.Handle);
                        }
                    }
                    else
                    {
                        ErrorStateEvent?.Invoke();
                        //如果是子实验
                        if (IsSubExperiment)
                        {
                            expFailedException = ex;
                        }
                        else
                            if (!AIControlled) MessageWindow.ShowTipWindow("实验发生异常,已结束：\n" + ex.Message, MainWindow.Handle);
                    }
                    expFailedException = ex;
                    //设置结束状态
                    SetStopState();
                }
            });
            ExpThread.Start();
        }
        private void ResumeEvent(object sender, RoutedEventArgs e)
        {
            ThreadResumeFlag = true;
            SetExpState("暂停实验...");
        }
        private void StopEvent(object sender, RoutedEventArgs e)
        {
            ThreadEndFlag = true;
            SetExpState("正在停止实验...");
        }

        #region 外部控制
        /// <summary>
        /// 开始实验
        /// </summary>
        public void Start()
        {
            StartEvent(null, new RoutedEventArgs());
        }
        /// <summary>
        /// 暂停实验
        /// </summary>
        public void Resume()
        {
            ResumeEvent(null, new RoutedEventArgs());
        }
        /// <summary>
        /// 停止实验
        /// </summary>
        public void Stop()
        {
            StopEvent(null, new RoutedEventArgs());
        }

        #endregion
        #endregion

        /// <summary>
        /// 线程终止标签
        /// </summary>
        private bool ThreadEndFlag { get; set; } = true;
        /// <summary>
        /// 线程暂停标签
        /// </summary>
        private bool ThreadResumeFlag { get; set; } = false;

        /// <summary>
        /// 线程状态判断函数
        /// </summary>
        public Action JudgeThreadEndOrResumeAction { get; set; } = null;
        /// <summary>
        /// 如果状态为等待则挂起，如果状态为结束则抛出异常
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void JudgeThreadEndOrResume()
        {
            if (ThreadEndFlag)
            {
                throw new ExpStopException();
            }
            if (ThreadResumeFlag)
            {
                SetResumeState();
                while (ThreadResumeFlag)
                {
                    if (ThreadEndFlag)
                    {
                        throw new ExpStopException();
                    }
                    Thread.Sleep(50);
                }
            }
        }



        public void Dispose()
        {
            if (ExpThread == null) return;
            ExpThread.Abort();
            while (ExpThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// 实验事件
        /// </summary>
        public abstract void ExperimentEvent();


        private ConfigType tempConfig = null;
        /// <summary>
        /// 参数读取事件
        /// </summary>
        public abstract ConfigType ReadConfig();

        /// <summary>
        /// 进行实验前的确认操作,如果不继续则返回false
        /// </summary>
        public abstract bool PreConfirmProcedure();

        /// <summary>
        /// 提供实验需要的设备,返回的对象必须是DeviceInfoBase类
        /// </summary>
        public abstract List<InfoBase> GetDevices();

    }
}

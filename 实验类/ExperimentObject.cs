using CodeHelper;
using Controls;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Label = System.Windows.Controls.Label;

namespace ODMR_Lab
{
    /// <summary>
    /// 实验基类型
    /// </summary>
    /// <typeparam name="ParamType"></typeparam>
    /// <typeparam name="ConfigType"></typeparam>
    public abstract class ExperimentObject<ParamType, ConfigType>
        where ParamType : ExpParamBase
        where ConfigType : ConfigBase
    {
        /// <summary>
        /// 非法字符
        /// </summary>
        public static List<char> InvaliidChars = new List<char>() { '$' };

        /// <summary>
        /// 实验文件类型
        /// </summary>
        public abstract ExperimentFileTypes ExpType { get; protected set; }

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

        public bool ReadFromExplorer()
        {
            FileObject obj = FileObject.FindFileFromExplorer();
            if (obj == null) return false;
            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                throw new Exception("此文件不属于实验类型文件");
            }

            if (ExpType == (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), obj.Descriptions["实验类型"]))
            {
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
            FileObject obj = InnerWrite();

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

            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
            }

            obj.SaveToFile(Path.Combine(filefolder, filename));
        }

        public bool WriteFromExplorer(string defaultName = "")
        {
            FileObject obj = InnerWrite();

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


            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
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

        /// <summary>
        /// 内部读操作，将obj中的信息转化成对应的ExperimentFileObject
        /// </summary>
        /// <param name="fobj"></param>
        protected abstract void InnerRead(FileObject fobj);

        /// <summary>
        /// 内部写操作，将ExperimentFileObject中的信息转化成FileObject
        /// </summary>
        /// <returns></returns>
        protected abstract FileObject InnerWrite();

        /// <summary>
        /// 转换成数据可视化对象
        /// </summary>
        /// <returns></returns>
        public abstract DataVisualSource ToDataVisualSource();

        #region 实验线程部分
        Thread ExpThread { get; set; } = null;

        /// <summary>
        /// 线程运行时的相关控件显示状态
        /// </summary>
        public List<KeyValuePair<FrameworkElement, RunningBehaviours>> ControlStates = new List<KeyValuePair<FrameworkElement, RunningBehaviours>>();

        #region 线程判断按钮
        DecoratedButton startButton = null;
        public DecoratedButton StartButton
        {
            get { return startButton; }
            set
            {
                startButton = value;
                startButton.Click -= StartEvent;
                startButton.Click += StartEvent;
            }
        }

        DecoratedButton resumeButton = null;
        public DecoratedButton ResumeButton
        {
            get { return resumeButton; }
            set
            {
                resumeButton = value;
                resumeButton.Click -= ResumeEvent;
                resumeButton.Click += ResumeEvent;
            }
        }

        DecoratedButton stopButton = null;
        public DecoratedButton StopButton
        {
            get { return stopButton; }
            set
            {
                stopButton = value;
                stopButton.Click -= StopEvent;
                stopButton.Click += StopEvent;
            }
        }

        public Label ExpStartTimeLabel { get; set; } = null;
        public Label ExpEndTimeLabel { get; set; } = null;

        /// <summary>
        /// 初始化事件，在读取参数和获取设备后触发
        /// </summary>
        public event Action InitEvent = null;

        public event Action ResumeStateEvent = null;
        public event Action EndStateEvent = null;
        public event Action ErrorStateEvent = null;

        private void SetStartState()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(StartButton, false);
                TrySetState(StopButton, true);
                TrySetState(ResumeButton, true);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning)
                        item.Key.IsHitTestVisible = true;
                    if (item.Value == RunningBehaviours.DisableWhenRunning)
                        item.Key.IsHitTestVisible = false;
                }
            });
        }

        private void SetResumeState()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(StartButton, true);
                TrySetState(StopButton, true);
                TrySetState(ResumeButton, false);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning)
                        item.Key.IsHitTestVisible = true;
                    if (item.Value == RunningBehaviours.DisableWhenRunning)
                        item.Key.IsHitTestVisible = false;
                }
                ResumeStateEvent?.Invoke();
            });
        }

        private void SetStopState()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                TrySetState(StartButton, true);
                TrySetState(StopButton, false);
                TrySetState(ResumeButton, false);
                foreach (var item in ControlStates)
                {
                    if (item.Value == RunningBehaviours.EnableWhenRunning)
                        item.Key.IsHitTestVisible = false;
                    if (item.Value == RunningBehaviours.DisableWhenRunning)
                        item.Key.IsHitTestVisible = true;
                }
                EndStateEvent?.Invoke();
            });
        }

        private void TrySetState(FrameworkElement ele, bool state)
        {
            if (ele == null) return;
            else
            {
                ele.IsEnabled = state;
            }
        }

        private void StartEvent(object sender, RoutedEventArgs e)
        {
            if (IsThreadResume == true && IsThreadEnd == false)
            {
                IsThreadResume = false;
                SetStartState();
                return;
            }
            SetStartState();
            if (PreConfirmProcedure() == false)
            {
                //设值结束状态
                SetStopState();
                return;
            }
            ExpThread = new Thread(() =>
            {
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
                        MessageWindow.ShowTipWindow("参数设置存在错误:\n" + ex.Message, MainWindow.Handle);
                        ErrorStateEvent?.Invoke();
                        SetStopState();
                        return;
                    }

                    List<InfoBase> Devices = new List<InfoBase>();
                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Devices = GetDevices();
                            DeviceDispatcher.UseDevices(Devices);
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageWindow.ShowTipWindow("设备获取失败:\n" + ex.Message, MainWindow.Handle);
                        ErrorStateEvent?.Invoke();
                        SetStopState();
                        return;
                    }

                    InitEvent?.Invoke();

                    //设置实验时间
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Param.SetStartTime(DateTime.Now);
                        if (ExpStartTimeLabel != null)
                        {
                            ExpStartTimeLabel.Content = Param.ExpStartTime.Value;
                        }
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = "";
                        }
                    });
                    IsThreadEnd = false;
                    IsThreadResume = false;
                    Config = tempConfig;
                    //进行试验
                    ExperimentEvent();
                    //设值结束状态
                    SetStopState();
                    //结束占用设备
                    DeviceDispatcher.EndUseDevices(Devices);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Param.SetEndTime(DateTime.Now);
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = Param.ExpEndTime.Value;
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageWindow.ShowTipWindow("定位过程发生异常,已结束定位过程：\n" + ex.Message, MainWindow.Handle);
                    //设值结束状态
                    SetStopState();
                    ErrorStateEvent?.Invoke();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Param.SetEndTime(DateTime.Now);
                        if (ExpEndTimeLabel != null)
                        {
                            ExpEndTimeLabel.Content = Param.ExpEndTime.Value;
                        }
                    });
                }
            });
            ExpThread.Start();
        }
        private void ResumeEvent(object sender, RoutedEventArgs e)
        {
            IsThreadResume = true;
        }
        private void StopEvent(object sender, RoutedEventArgs e)
        {
            IsThreadEnd = true;
        }
        #endregion

        /// <summary>
        /// 线程终止标签
        /// </summary>
        public bool IsThreadEnd { get; private set; }
        /// <summary>
        /// 线程暂停标签
        /// </summary>
        public bool IsThreadResume { get; private set; }
        /// <summary>
        /// 如果状态为等待则挂起，如果状态为结束则抛出异常
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void JudgeThreadEndOrResume()
        {
            if (IsThreadEnd)
            {
                throw new Exception("定位进程已被终止");
            }
            if (IsThreadResume)
            {
                SetResumeState();
                while (IsThreadResume)
                {
                    if (IsThreadEnd)
                    {
                        throw new Exception("定位进程已被终止");
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
        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CodeHelper;
using Controls.Windows;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.位移台界面.参数;
using ODMR_Lab.实验部分.场效应器件测量;
using ODMR_Lab.实验部分.温度监测;
using ODMR_Lab.实验部分.设备参数监测;
using ODMR_Lab.激光控制;
using ODMR_Lab.设备部分.光子探测器;

namespace ODMR_Lab.IO操作
{
    /// <summary>
    /// 参数管理类
    /// </summary>
    internal class ParamManager
    {
        /// <summary>
        /// 保存参数
        /// </summary>
        public static void SaveParams(MessageWindow window = null)
        {
            #region 参数监测配置
            WindowHelper.SetContent(window, "正在保存: " + "设备参数监测配置");

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "DeviceListenerConfig")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "DeviceListenerConfig"));
            }
            try
            {
                MainWindow.Exp_DevParamListenPage.ListenDispatcher.WriteToFile(Path.Combine(Environment.CurrentDirectory, "DeviceListenerConfig", "ConfigData.userdat"));
            }
            catch (Exception)
            {
            }

            WindowHelper.SetContent(window, "正在保存: " + "设备参数设置配置");

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "DeviceSetConfig")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "DeviceSetConfig"));
            }
            try
            {
                MainWindow.Exp_DevParamSetPage.WriteToFile(Path.Combine(Environment.CurrentDirectory, "DeviceSetConfig", "ConfigData.userdat"));
            }
            catch (Exception)
            {
            }
            #endregion

            FileObject fobj = new FileObject();

            WindowHelper.SetContent(window, "正在保存: " + "设备参数");
            #region 设备参数
            ParamListenerConfigParams PP = new ParamListenerConfigParams();
            PP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_DevParamListenPage }, false);
            WriteParamToFile(PP, fobj);
            #endregion

            WindowHelper.SetContent(window, "正在保存: " + "场效应器件测量参数");
            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            IVP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            VP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            WriteParamToFile(IVP, fobj);
            WriteParamToFile(VP, fobj);
            #endregion

            WindowHelper.SetContent(window, "正在保存: " + "位移台配置信息");
            #region 位移台配置信息(是否反转) 
            App.Current.Dispatcher.Invoke(() =>
            {
                StageControlConfigParams SCP = new StageControlConfigParams();
                SCP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_StagePage.ProbePanel }, false);
                WriteParamToFile(SCP, fobj, MainWindow.Exp_StagePage.ProbePanel.Name);
                SCP = new StageControlConfigParams();
                SCP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_StagePage.MagnetPanel }, false);
                WriteParamToFile(SCP, fobj, MainWindow.Exp_StagePage.MagnetPanel.Name);
                SCP = new StageControlConfigParams();
                SCP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_StagePage.SamplePanel }, false);
                WriteParamToFile(SCP, fobj, MainWindow.Exp_StagePage.SamplePanel.Name);
                SCP = new StageControlConfigParams();
                SCP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_StagePage.MWPanel }, false);
                WriteParamToFile(SCP, fobj, MainWindow.Exp_StagePage.MWPanel.Name);
                SCP = new StageControlConfigParams();
                SCP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_StagePage.LenPanel }, false);
                WriteParamToFile(SCP, fobj, MainWindow.Exp_StagePage.LenPanel.Name);
            });
            #endregion

            WindowHelper.SetContent(window, "正在保存: " + "Trace参数");
            #region Trace参数
            TraceConfigParams APD1 = new TraceConfigParams();
            APD1.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_TracePage }, false);
            WriteParamToFile(APD1, fobj);
            #endregion

            WindowHelper.SetContent(window, "正在保存: " + "ODMR全局参数");
            #region ODMR全局参数
            ODMRConfigParams ODMRC = new ODMRConfigParams();
            ODMRC.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SequencePage }, false);
            WriteParamToFile(ODMRC, fobj);
            #endregion

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "UIParam")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "UIParam"));
            }
            fobj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat"));

            WindowHelper.SetContent(window, "正在保存参数: " + "ODMR实验");
            #region ODMR实验

            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "ODMRConfig")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "ODMRConfig"));
            }
            int ind = 0;
            foreach (var item in MainWindow.Exp_SequencePage.ExpObjects)
            {
                try
                {
                    WindowHelper.SetContent(window, "正在保存参数: " + "ODMR实验  " + ind.ToString() + "/" + MainWindow.Exp_SequencePage.ExpObjects.Count.ToString());
                    FileObject ODMRSaveFile = new FileObject();
                    item.ReadFromPageAndWriteConfigToFile(ODMRSaveFile);
                    string path = Path.Combine(Environment.CurrentDirectory, "ODMRConfig", FileHelper.Combine("_", item.ODMRExperimentGroupName, item.ODMRExperimentName + ".userdat"));
                    if (FileHelper.IsFileStrValid(path))
                        ODMRSaveFile.SaveToFile(path);
                    else
                    {
                        int k = 0;
                    }
                }
                catch (Exception)
                {
                }
                ++ind;
            }
            #endregion

            #region 自定义算法参数
            WindowHelper.SetContent(window, "正在保存参数:" + "自定义算法参数");
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "AlgorithmConfig")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "AlgorithmConfig"));
            }
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MainWindow.Exp_AlgorithmPage.SaveToFile(Path.Combine(Environment.CurrentDirectory, "AlgorithmConfig", "Config.userdat"));
                }
                catch (Exception)
                {
                }
            });
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ReadAndLoadParams(MessageWindow window = null)
        {
            FileObject fobj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat"));

            #region 参数监测配置
            WindowHelper.SetContent(window, "正在读取: " + "设备参数监测配置");
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MainWindow.Exp_DevParamListenPage.ListenDispatcher = new DeviceListenerDispatcher(MainWindow.Exp_DevParamListenPage, Path.Combine(Environment.CurrentDirectory, "DeviceListenerConfig", "ConfigData.userdat"));
                    MainWindow.Exp_DevParamListenPage.ListenDispatcher.StartListen();
                    MainWindow.Exp_DevParamListenPage.ValidateParamBars();
                }
                catch (Exception)
                {
                    MainWindow.Exp_DevParamListenPage.ListenDispatcher = new DeviceListenerDispatcher(MainWindow.Exp_DevParamListenPage, "");
                    MainWindow.Exp_DevParamListenPage.ListenDispatcher.StartListen();
                    MainWindow.Exp_DevParamListenPage.ValidateParamBars();
                }
                ParamListenerConfigParams PP = new ParamListenerConfigParams();
                ReadFromFile(PP, fobj);
                PP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_DevParamListenPage }, false);
                MainWindow.Exp_DevParamListenPage.SetSampleConfigs();
            });

            WindowHelper.SetContent(window, "正在读取: " + "设备参数设置配置");
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MainWindow.Exp_DevParamSetPage.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "DeviceSetConfig", "ConfigData.userdat"));
                    MainWindow.Exp_DevParamSetPage.ValidatePanel();
                }
                catch (Exception)
                {
                }
            });
            #endregion

            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat")))
            {
                return;
            }

            WindowHelper.SetContent(window, "正在读取:" + "设备参数");
            #region 设备参数
            #endregion

            #region 温度监控参数
            #endregion

            WindowHelper.SetContent(window, "正在读取:" + "场效应器件测量参数");
            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            ReadFromFile(IVP, fobj);
            IVP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            ReadFromFile(VP, fobj);
            VP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            #endregion

            WindowHelper.SetContent(window, "正在读取:" + "位移台控制参数");
            #region 位移台控制参数
            App.Current.Dispatcher.Invoke(() =>
            {
                StageControlConfigParams SCP = new StageControlConfigParams();
                ReadFromFile(SCP, fobj, MainWindow.Exp_StagePage.ProbePanel.Name);
                SCP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_StagePage.ProbePanel }, false);
                SCP = new StageControlConfigParams();
                ReadFromFile(SCP, fobj, MainWindow.Exp_StagePage.MagnetPanel.Name);
                SCP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_StagePage.MagnetPanel }, false);
                SCP = new StageControlConfigParams();
                ReadFromFile(SCP, fobj, MainWindow.Exp_StagePage.SamplePanel.Name);
                SCP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_StagePage.SamplePanel }, false);
                SCP = new StageControlConfigParams();
                ReadFromFile(SCP, fobj, MainWindow.Exp_StagePage.MWPanel.Name);
                SCP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_StagePage.MWPanel }, false);
                SCP = new StageControlConfigParams();
                ReadFromFile(SCP, fobj, MainWindow.Exp_StagePage.LenPanel.Name);
                SCP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_StagePage.LenPanel }, false);
            });
            #endregion


            WindowHelper.SetContent(window, "正在读取:" + "ODMR实验");
            #region ODMR实验
            ODMRConfigParams ODMRP = new ODMRConfigParams();
            ReadFromFile(ODMRP, fobj);
            ODMRP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SequencePage });
            #endregion


            #region ODMR实验
            int index = 1;
            foreach (var item in MainWindow.Exp_SequencePage.ExpObjects)
            {
                string path = Path.Combine(Environment.CurrentDirectory, "ODMRConfig", FileHelper.Combine("_", item.ODMRExperimentGroupName, item.ODMRExperimentName) + ".userdat");
                if (!FileHelper.IsFileStrValid(path)) continue;
                WindowHelper.SetContent(window, "正在读取:" + "ODMR实验  " + index.ToString() + "/" + MainWindow.Exp_SequencePage.ExpObjects.Count.ToString() + "个");
                if (File.Exists(path))
                {
                    if (item.ODMRExperimentName == "退相干时间测量(T2)")
                    {
                        int k = 1;
                    }
                    FileObject ODMRSaveFile = FileObject.ReadFromFile(path);
                    item.ReadFromFileAndLoadToPage(ODMRSaveFile);
                }
                ++index;
            }
            #endregion

            WindowHelper.SetContent(window, "正在读取:" + "全局序列参数");
            #region 全局序列参数
            GlobalPulseParams.ReadFromFile();
            #endregion

            #region 自定义算法参数
            WindowHelper.SetContent(window, "正在读取:" + "自定义算法参数");
            App.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MainWindow.Exp_AlgorithmPage.LoadFromFile(Path.Combine(Environment.CurrentDirectory, "AlgorithmConfig", "Config.userdat"));
                }
                catch (Exception)
                {
                }
            });
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Base"></param>
        /// <param name="fobj"></param>
        /// <param name="identify">标识符，当同一个控件中存在多个相同子控件，并且需要写入文件的属性在子空间中时需要使用设置此参数进行区分</param>
        private static void WriteParamToFile(ConfigBase Base, FileObject fobj, string identify = "")
        {
            Dictionary<string, string> value = Base.GenerateTaggedDescriptions(identify);
            foreach (var item in value)
            {
                fobj.Descriptions.Add(item.Key, item.Value);
            }
        }

        private static void ReadFromFile(ConfigBase Base, FileObject fobj, string identify = "")
        {
            Base.ReadFromTaggedDescriptions(fobj.Descriptions, identify);
        }
    }
}

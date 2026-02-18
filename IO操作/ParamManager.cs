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
using ODMR_Lab.激光控制;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.源表;

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
        public static void SaveParams()
        {
            FileObject fobj = new FileObject();

            #region 设备参数

            PowerMeterDevConfigParams P1 = new PowerMeterDevConfigParams();
            P1.ReadFromPage(new FrameworkElement[] { MainWindow.Dev_PowerMeterPage }, false);
            WriteParamToFile(P1, fobj);
            #endregion

            #region 温度监控参数
            TemperatureConfigParams TP = new TemperatureConfigParams();
            TP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_TemPeraPage, MainWindow.Exp_TemPeraPage.SetWindow }, false);
            WriteParamToFile(TP, fobj);
            #endregion

            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            IVP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            VP.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            WriteParamToFile(IVP, fobj);
            WriteParamToFile(VP, fobj);
            #endregion

            #region 位移台控制参数
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
            #endregion

            #region Trace参数
            TraceConfigParams APD1 = new TraceConfigParams();
            APD1.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_TracePage }, false);
            WriteParamToFile(APD1, fobj);
            #endregion

            #region ODMR全局参数
            ODMRConfigParams ODMRC = new ODMRConfigParams();
            ODMRC.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_SequencePage }, false);
            WriteParamToFile(ODMRC, fobj);
            #endregion

            #region ODMR实验
            FileObject ODMRSaveFile = new FileObject();
            foreach (var item in MainWindow.Exp_SequencePage.ExpObjects)
            {
                item.ReadFromPageAndWriteConfigToFile(ODMRSaveFile);
            }
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData"));
                var str = File.Create(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat"));
                str.Close();
            }
            ODMRSaveFile.SaveToFile(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat"));
            #endregion


            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "UIParam")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "UIParam"));
            }
            fobj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat"));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ReadAndLoadParams(MessageWindow window = null)
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat")))
            {
                return;
            }
            FileObject fobj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat"));

            WindowHelper.SetContent(window, "正在读取保存参数:" + "设备参数");
            #region 设备参数
            PowerMeterDevConfigParams P1 = new PowerMeterDevConfigParams();
            ReadFromFile(P1, fobj);
            P1.LoadToPage(new FrameworkElement[] { MainWindow.Dev_PowerMeterPage }, false);
            #endregion

            WindowHelper.SetContent(window, "正在读取保存参数:" + "温度监控参数");
            #region 温度监控参数
            TemperatureConfigParams TP = new TemperatureConfigParams();
            ReadFromFile(TP, fobj);
            TP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_TemPeraPage, MainWindow.Exp_TemPeraPage.SetWindow }, false);
            #endregion

            WindowHelper.SetContent(window, "正在读取保存参数:" + "场效应器件测量参数");
            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            ReadFromFile(IVP, fobj);
            IVP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            ReadFromFile(VP, fobj);
            VP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            #endregion

            WindowHelper.SetContent(window, "正在读取保存参数:" + "位移台控制参数");
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


            WindowHelper.SetContent(window, "正在读取保存参数:" + "ODMR实验");
            #region ODMR实验
            ODMRConfigParams ODMRP = new ODMRConfigParams();
            ReadFromFile(ODMRP, fobj);
            ODMRP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SequencePage });
            #endregion


            #region ODMR实验
            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat")))
            {
                FileObject ODMRSaveFile = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "Sequence", "ConfigData", "ConfigParams.userdat"));
                int index = 1;
                foreach (var item in MainWindow.Exp_SequencePage.ExpObjects)
                {
                    WindowHelper.SetContent(window, "正在读取保存参数:" + "ODMR实验  " + index.ToString() + "/" + MainWindow.Exp_SequencePage.ExpObjects.Count.ToString() + "个");
                    item.ReadFromFileAndLoadToPage(ODMRSaveFile);
                    ++index;
                }
            }
            #endregion

            WindowHelper.SetContent(window, "正在读取保存参数:" + "全局序列参数");
            #region 全局序列参数
            GlobalPulseParams.ReadFromFile();
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

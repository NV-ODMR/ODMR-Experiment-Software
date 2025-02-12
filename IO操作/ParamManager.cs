using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CodeHelper;
using ODMR_Lab.实验部分.位移台界面.参数;
using ODMR_Lab.实验部分.场效应器件测量;
using ODMR_Lab.实验部分.温度监测;
using ODMR_Lab.磁场调节;
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

            #region 磁场调节参数
            MagnetScanConfigParams P = new MagnetScanConfigParams();
            P.ReadFromPage(new FrameworkElement[] { MainWindow.Exp_MagnetControlPage }, false);
            WriteParamToFile(P, fobj);
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
            #endregion

            #region 自定义实验
            MemberInfo[] info = typeof(MainWindow).GetMembers();
            foreach (var item in info)
            {
                ODMR_Lab.自定义实验.DisplayPage page = null;
                if (item.ReflectedType.Name == typeof(ODMR_Lab.自定义实验.DisplayPage).Name)
                {
                    if (item.MemberType == MemberTypes.Field)
                    {
                        page = (ODMR_Lab.自定义实验.DisplayPage)((FieldInfo)item).GetValue(MainWindow.Handle);
                    }
                    if (item.MemberType == MemberTypes.Property)
                    {
                        page = (ODMR_Lab.自定义实验.DisplayPage)((PropertyInfo)item).GetValue(MainWindow.Handle);
                    }
                    自定义实验.CustomConfigParams p = (自定义实验.CustomConfigParams)page.ExpObject.Config.Copy();
                    p.ReadFromPage(new FrameworkElement[] { page }, false);
                    WriteParamToFile(p, fobj);
                }
            }
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
        public static void ReadAndLoadParams()
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat")))
            {
                return;
            }
            FileObject fobj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "UIParam", "Param.userdat"));

            #region 设备参数
            PowerMeterDevConfigParams P1 = new PowerMeterDevConfigParams();
            ReadFromFile(P1, fobj);
            P1.LoadToPage(new FrameworkElement[] { MainWindow.Dev_PowerMeterPage }, false);
            #endregion

            #region 磁场调节参数
            MagnetScanConfigParams P = new MagnetScanConfigParams();
            ReadFromFile(P, fobj);
            P.LoadToPage(new FrameworkElement[] { MainWindow.Exp_MagnetControlPage });
            #endregion

            #region 温度监控参数
            TemperatureConfigParams TP = new TemperatureConfigParams();
            ReadFromFile(TP, fobj);
            TP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_TemPeraPage, MainWindow.Exp_TemPeraPage.SetWindow }, false);
            #endregion

            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            ReadFromFile(IVP, fobj);
            IVP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            ReadFromFile(VP, fobj);
            VP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage }, false);
            #endregion

            #region 位移台控制参数
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
            #endregion

            #region 自定义实验
            MemberInfo[] info = typeof(MainWindow).GetMembers();
            foreach (var item in info)
            {
                ODMR_Lab.自定义实验.DisplayPage page = null;
                if (item.ReflectedType.Name == typeof(ODMR_Lab.自定义实验.DisplayPage).Name)
                {
                    if (item.MemberType == MemberTypes.Field)
                    {
                        page = (ODMR_Lab.自定义实验.DisplayPage)((FieldInfo)item).GetValue(MainWindow.Handle);
                    }
                    if (item.MemberType == MemberTypes.Property)
                    {
                        page = (ODMR_Lab.自定义实验.DisplayPage)((PropertyInfo)item).GetValue(MainWindow.Handle);
                    }
                    自定义实验.CustomConfigParams p = (自定义实验.CustomConfigParams)page.ExpObject.Config.Copy();
                    ReadFromFile(p, fobj);
                    p.LoadToPage(new FrameworkElement[] { page }, false);
                }
            }
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

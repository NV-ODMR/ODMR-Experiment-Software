using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CodeHelper;
using ODMR_Lab.实验部分.场效应器件测量;
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
            P1.LoadToPage(new FrameworkElement[] { MainWindow.Dev_PowerMeterPage });
            #endregion

            #region 磁场调节参数
            MagnetScanConfigParams P = new MagnetScanConfigParams();
            ReadFromFile(P, fobj);
            P.LoadToPage(new FrameworkElement[] { MainWindow.Exp_MagnetControlPage });
            #endregion

            #region 温度监控参数
            TemperatureConfigParams TP = new TemperatureConfigParams();
            ReadFromFile(TP, fobj);
            TP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_TemPeraPage, MainWindow.Exp_TemPeraPage.SetWindow });
            #endregion

            #region 场效应器件测量参数
            IVMeasureConfigParams IVP = new IVMeasureConfigParams();
            ReadFromFile(IVP, fobj);
            IVP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage });
            VoltageSetConfigParams VP = new VoltageSetConfigParams();
            ReadFromFile(VP, fobj);
            VP.LoadToPage(new FrameworkElement[] { MainWindow.Exp_SourcePage });
            #endregion
        }

        private static void WriteParamToFile(ConfigBase Base, FileObject fobj)
        {
            Dictionary<string, string> value = Base.GenerateTaggedDescriptions();
            foreach (var item in value)
            {
                fobj.Descriptions.Add(item.Key, item.Value);
            }
        }

        private static void ReadFromFile(ConfigBase Base, FileObject fobj)
        {
            Base.ReadFromTaggedDescriptions(fobj.Descriptions);
        }
    }
}

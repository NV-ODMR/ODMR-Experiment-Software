using CodeHelper;
using Controls.Windows;
using MathNet.Numerics.Distributions;
using ODMR_Lab.Windows;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using System.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// OffsetWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PredictWindow : Window
    {

        private MagnetLoc exp = null;

        public PredictWindow(MagnetLoc parentexp)
        {
            exp = parentexp;
            InitializeComponent();
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterWindow(this, null, null, null, 0, 30);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// 计算偏移量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calc(object sender, RoutedEventArgs e)
        {
            try
            {
                PredictData d = new PredictData(
                    double.Parse(XLoc.Content.ToString()),
                    double.Parse(YLoc.Content.ToString()),
                    double.Parse(ZLoc.Content.ToString()),
                    10.9425,
                    //double.Parse(ZDistance.Content.ToString()),
                    double.Parse(CheckedTheta.Content.ToString()),
                    double.Parse(CheckedPhi.Content.ToString()),
                    double.Parse(PredictTheta.Text),
                    double.Parse(PredictPhi.Text),
                    double.Parse(PredictZ.Text));
                exp.CalculatePredictField(d);
                PreX.Content = Math.Round(d.XLocPredictOutPut, 5).ToString();
                PreY.Content = Math.Round(d.YLocPredictOutPut, 5).ToString();
                PreA.Content = Math.Round(d.ALocPredictOutPut, 5).ToString();
                PreB.Content = Math.Round(d.PredictB, 5).ToString();
            }
            catch (Exception ex) { return; }
        }

        /// <summary>
        /// 导入文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadFile(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                SequenceFileExpObject fobj = new SequenceFileExpObject();
                fobj.ReadFromFile(dlg.FileName);
                if (fobj.ODMRExperimentGroupName != exp.ODMRExperimentGroupName || fobj.ODMRExperimentName != exp.ODMRExperimentName)
                {
                    throw new Exception();
                }
                //导入输出参数
                foreach (var item in fobj.OutputParams)
                {
                    item.PropertyName = item.PropertyName.Replace("Output_", "");
                    item.LoadToPage(new FrameworkElement[] { this }, false);
                }
                FileName.Content = Path.GetFileName(dlg.FileName);
                FileName.ToolTip = Path.GetFileName(dlg.FileName);
            }
            catch (Exception)
            {
                MessageWindow.ShowTipWindow("要打开的文件不是支持的类型", this);
            }
        }

        private void MoveToLoc(object sender, RoutedEventArgs e)
        {
            try
            {
                double x = double.Parse(PreX.Content.ToString());
                double y = double.Parse(PreY.Content.ToString());
                double z = double.Parse(PredictZ.Text.ToString());
                double a = double.Parse(PreA.Content.ToString());
                Thread t = new Thread(() =>
                {
                    MessageWindow win = null;
                    try
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            win = new MessageWindow("提示", "正在移动...", MessageBoxButton.OK, false, false);
                            win.ShowDialog();
                        });
                        NanoStageInfo xdev = exp.GetDeviceByName("MagnetX") as NanoStageInfo;
                        NanoStageInfo ydev = exp.GetDeviceByName("MagnetY") as NanoStageInfo;
                        NanoStageInfo zdev = exp.GetDeviceByName("MagnetZ") as NanoStageInfo;
                        NanoStageInfo adev = exp.GetDeviceByName("MagnetAngle") as NanoStageInfo;
                        DeviceDispatcher.UseDevices(xdev, ydev, zdev, adev);
                        xdev.Device.MoveToAndWait(x, 10000);
                        ydev.Device.MoveToAndWait(y, 10000);
                        zdev.Device.MoveToAndWait(z, 10000);
                        adev.Device.MoveToAndWait(a, 60000);
                        DeviceDispatcher.EndUseDevices(xdev, ydev, zdev, adev);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            win.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageWindow.ShowTipWindow("移动未完成\n" + ex.Message, this);
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            win.Close();
                        });
                    }
                });
                t.Start();
            }
            catch (Exception ex)
            {
                MessageWindow.ShowTipWindow("移动未完成\n" + ex.Message, this);
            }
        }
    }
}

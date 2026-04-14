using CodeHelper;
using Controls;
using Controls.Charts;
using Controls.Windows;
using HardWares;
using HardWares.APD;
using HardWares.APD.Exclitas_SPCM_AQRH;
using HardWares.Windows;
using HardWares.仪器列表.电动翻转座;
using HardWares.温度控制器;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.源表;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using HardWares.端口基类部分;
using HardWares.纳米位移台;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.基本窗口;
using ODMR_Lab.实验部分.参数设置面板;
using ODMR_Lab.实验部分.设备参数监测;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.板卡;
using ODMR_Lab.设备部分.相机_翻转镜;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = Controls.ContextMenu;

namespace ODMR_Lab.实验部分.设备参数面板
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "参数设置面板";

        public List<ParamSetInfo> ParamList = new List<ParamSetInfo>();

        public DisplayPage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
        }

        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }

        private void NewParam(object sender, RoutedEventArgs e)
        {
            //选择设备
            DeviceFindWindow w = new DeviceFindWindow();
            w.Owner = Window.GetWindow(this);
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var devresult = w.ShowDialog();
            if (devresult.Key == null && devresult.Value == null) return;
            DeviceParamSelectWindow win = new DeviceParamSelectWindow();
            win.Owner = Window.GetWindow(this);
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Parameter target = null;
            if (devresult.Value == null)
            {
                target = win.ShowDialog(devresult.Key);
            }
            else
            {
                target = win.ShowDialog(devresult.Value);
            }
            if (target == null) return;
            //新建参数
            ParamSetInfo info = new ParamSetInfo(this, devresult.Key, devresult.Value, target);
            info.ValidateDev(devresult.Key, devresult.Value, target);
            info.ParamBar.LoadParam();
            AppendInfoCommands(info);
            ParamList.Add(info);
            ParamPanel.Children.Add(info.ParamBar);
        }

        public void AppendInfoCommands(ParamSetInfo info)
        {
            info.ParamBar.ParamDeleteEvent += new Action<ParamSetBar>((ele) =>
            {
                if (MessageWindow.ShowMessageBox("删除", "确定要取消此参数吗?", MessageBoxButton.YesNo, owner: Window.GetWindow(this)) == MessageBoxResult.Yes)
                {
                    //删除
                    ParamList.Remove(info);
                    ParamPanel.Children.Remove(info.ParamBar);
                }
            });
            info.ParamBar.SelectedEvent += new Action<ParamSetBar>((p) =>
            {
                foreach (var item in ParamPanel.Children)
                {
                    (item as ParamSetBar).Background = new SolidColorBrush(Colors.Transparent);
                }
                p.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3F85CD"));
            });
        }

        public void ValidatePanel()
        {
            ParamPanel.Children.Clear();
            foreach (var item in ParamList)
            {
                ParamPanel.Children.Add(item.ParamBar);
            }
        }

        #region 文件操作
        public void ReadFromFile(string filepath)
        {
            FileObject obj = FileObject.ReadFromFile(filepath);

            var devproductnames = obj.ExtractString("ProductName");
            var devproductidentifiers = obj.ExtractString("ProductIdentifier");
            var haschannel = obj.ExtractBoolean("HasChannel");
            var channelnames = obj.ExtractString("ChannelName");
            var parameters = obj.ExtractString("ParameterName");
            var parameterdescs = obj.ExtractString("ParameterDescription");

            ParamList.Clear();
            for (int i = 0; i < devproductnames.Count; i++)
            {
                try
                {
                    ParamSetInfo info = new ParamSetInfo(this, devproductnames[i], devproductidentifiers[i], haschannel[i] ? channelnames[i] : "", parameters[i], parameterdescs[i]);
                    AppendInfoCommands(info);
                    ParamList.Add(info);

                    PortObject dev = PortObject.FindDevice(devproductidentifiers[i], devproductnames[i]);
                    if (dev == null) continue;
                    PortElement channel = null;
                    if (haschannel[i])
                    {
                        channel = (dev as ElementPortObject).Channels.Where((x) => x.ChannelName == channelnames[i]).ElementAt(0);
                    }
                    Parameter p = null;
                    if (channel != null)
                    {
                        p = channel.AvailableParameterNames().Where((x) => x.ParameterName == parameters[i]).ElementAt(0);
                    }
                    else
                    {
                        p = dev.AvailableParameterNames().Where((x) => x.ParameterName == parameters[i]).ElementAt(0);
                    }

                    info.ValidateDev(dev, channel, p);
                    info.ParamBar.LoadParam();
                }
                catch (Exception)
                {

                }
            }
        }

        public void WriteToFile(string filepath)
        {
            FileObject obj = new FileObject();
            var devproductnames = ParamList.Select(p => p.Device.ProductName).ToList();
            var devproductidentifiers = ParamList.Select(p => p.Device.ProductIdentifier).ToList();

            var haschannel = ParamList.Select(p => p.Channel == null ? false : true).ToList();
            var channelnames = ParamList.Select(p => p.Channel == null ? "NoChannel" : p.Channel.ChannelName).ToList();

            var parameters = ParamList.Select(p => p.TargetParameter.ParameterName).ToList();
            var parameterdescs = ParamList.Select(p => p.TargetParameter.Description).ToList();


            obj.WriteStringData("ProductName", devproductnames);
            obj.WriteStringData("ProductIdentifier", devproductidentifiers);
            obj.WriteBooleanData("HasChannel", haschannel);
            obj.WriteStringData("ChannelName", channelnames);
            obj.WriteStringData("ParameterName", parameters);
            obj.WriteStringData("ParameterDescription", parameterdescs);

            obj.SaveToFile(filepath);
        }
        #endregion
    }
}

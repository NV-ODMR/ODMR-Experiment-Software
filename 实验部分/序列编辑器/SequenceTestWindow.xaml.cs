using CodeHelper;
using Controls;
using Controls.Windows;
using HardWares.仪器列表.板卡.Spincore_PulseBlaster;
using HardWares.板卡;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ODMR_Lab.实验部分.序列编辑器
{
    /// <summary>
    /// GlobalSequenceWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SequenceTestWindow : Window
    {
        SequenceDataAssemble seq = null;
        public SequenceTestWindow()
        {
            InitializeComponent();
            WindowResizeHelper h = new WindowResizeHelper();
            h.RegisterWindow(this, null, null, null, 0, 30);
        }

        public new void Show(SequenceDataAssemble sequence)
        {
            seq = sequence;
            SequenceName.Content = seq.Name;
            SequenceName.ToolTip = seq.Name;
            Show();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            if (Devices.SelectedItem == null) return;
            try
            {
                //使用PulseBlaster
                var dev = Devices.SelectedItem.Tag as PulseBlasterInfo;
                DeviceDispatcher.UseDevices(dev);
                List<CommandBase> lines = new List<CommandBase>();
                seq.LoopCount = int.Parse(Loops.Text);
                seq.AddToCommandLine(lines, out string informs);
                dev.Device.SetCommands(lines);
                dev.Device.Start();

                StartBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;
                IsHitTestVisible = false;
            }
            catch (Exception ex)
            {
                IsHitTestVisible = true;
                MessageWindow.ShowTipWindow("无法输出序列:\n" + ex.Message, this);
            }
        }

        private void UpdatePulseBlaster(object sender, RoutedEventArgs e)
        {
            Devices.TemplateButton = Devices;
            Devices.UpdateItems(DeviceDispatcher.GetDevice(DeviceTypes.PulseBlaster).Select(x =>
            {
                var dev = x as PulseBlasterInfo;
                DecoratedButton btn = new DecoratedButton() { Text = dev.GetDeviceDescription() };
                btn.Tag = dev;
                return btn;
            }));
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            IsHitTestVisible = true;
            var dev = Devices.SelectedItem.Tag as PulseBlasterInfo;
            dev.Device.End();
            DeviceDispatcher.EndUseDevices(Devices.SelectedItem.Tag as PulseBlasterInfo);
        }
    }
}

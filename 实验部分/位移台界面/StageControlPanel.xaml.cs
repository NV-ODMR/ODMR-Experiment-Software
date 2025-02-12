using Controls;
using HardWares.纳米位移台;
using ODMR_Lab.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ODMR_Lab.实验部分.位移台界面
{
    /// <summary>
    /// StageControlPanel.xaml 的交互逻辑
    /// </summary>
    public partial class StageControlPanel : Grid
    {
        public StageControlPanel()
        {
            InitializeComponent();
        }

        public PartTypes MoverPart = PartTypes.None;


        public Thread MoveThread = null;
        private void InnerMove(MoverTypes type, double step, bool ispositive, bool isreverse)
        {
            try
            {
                var stage = DeviceDispatcher.GetMoverDevice(type, MoverPart);
                if (stage == null) return;

                if (MoveThread == null || MoveThread.ThreadState != ThreadState.Running)
                {
                    MoveThread = new Thread(() =>
                    {
                        DeviceDispatcher.UseDevices(stage);
                        stage.Device.MoveStepAndWait((ispositive ? 1 : -1) * step * (isreverse ? 1 : -1), 60000);
                        DeviceDispatcher.EndUseDevices(stage);
                    });
                    MoveThread.Start();
                    return;
                }

            }
            catch (Exception)
            {
                return;
            }
        }

        private void XNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, XYSelector.CurrentValue, false, ReverseX.IsSelected);
        }
        private void XPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, XYSelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void YNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, XYSelector.CurrentValue, false, ReverseX.IsSelected);
        }
        private void YPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, XYSelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void ZNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ZSelector.CurrentValue, false, ReverseX.IsSelected);
        }
        private void ZPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ZSelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void AngleXPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ASelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void AngleXNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ASelector.CurrentValue, false, ReverseX.IsSelected);
        }

        private void AngleYPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ASelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void AngleYNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ASelector.CurrentValue, false, ReverseX.IsSelected);
        }

        private void AngleZPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.X, ASelector.CurrentValue, true, ReverseX.IsSelected);
        }

        private void AngleZNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleZ, ASelector.CurrentValue, false, ReverseX.IsSelected);
        }
    }
}

using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

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

        /// <summary>
        /// 在 UI 线程判定本控件是否可见（页面被切走 / 独立窗口隐藏 / 宿主窗口最小化时返回 false）。
        /// WPF 可视性只能在 UI 线程安全读取，故非 UI 线程经 Dispatcher 询问。
        /// </summary>
        private bool CanPollDevice()
        {
            try
            {
                if (Dispatcher.CheckAccess()) return CheckVisibleCore();
                return Dispatcher.Invoke(new Func<bool>(CheckVisibleCore));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 可见性判定本体（必须运行在 UI 线程）
        /// </summary>
        private bool CheckVisibleCore()
        {
            if (!IsVisible) return false;
            Window w = Window.GetWindow(this);
            if (w != null && w.WindowState == WindowState.Minimized) return false;
            return true;
        }

        /// <summary>
        /// 读取并显示位移台位置（5 个面板 × 最多 6 轴 × 每轴 1 次设备读）。
        /// 轮询治理（2026-10-05）：加入可见性门控 —— 控件不可见时直接返回，不读任何设备（判据 V6）；
        /// 刷新间隔同时由 Page.xaml.cs 的 ListenerGapMs 从 50ms 放宽到 200ms。
        /// </summary>
        public void UpdateListenerState()
        {
            if (!CanPollDevice()) return;
            string xv = "";
            string yv = "";
            string zv = "";
            string axv = "";
            string ayv = "";
            string azv = "";
            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.X, MoverPart);
                if (dev != null)
                    xv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.Y, MoverPart);
                if (dev != null)
                    yv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.Z, MoverPart);
                if (dev != null)
                    zv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleX, MoverPart);
                if (dev != null)
                    axv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleY, MoverPart);
                if (dev != null)
                    ayv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            try
            {
                var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleZ, MoverPart);
                if (dev != null)
                    azv = dev.Device.Position.ToString();
            }
            catch (Exception) { }

            Dispatcher.Invoke(() =>
            {
                XYLocs.Text = "X:  " + xv + "  Y:  " + yv;
                ZLocs.Text = "Z:  " + zv;
                AngleLocs.Text = "X:  " + axv + "  Y:  " + ayv + "  Z:  " + azv;
            });
        }


        public PartTypes MoverPart = PartTypes.None;


        public Thread MoveThread = null;
        private void InnerMove(MoverTypes type, double step, bool ispositive, bool isreverse)
        {
            try
            {
                var stage = DeviceDispatcher.GetMoverDevice(type, MoverPart);
                if (stage == null) return;
                if (MoveThread == null || MoveThread.ThreadState == ThreadState.Stopped)
                {
                    MoveThread = new Thread(() =>
                    {
                        try
                        {
                            DeviceDispatcher.UseDevices(stage);
                            stage.Device.MoveStepAndWait((ispositive ? 1 : -1) * step * (isreverse ? -1 : 1), 50);
                            DeviceDispatcher.EndUseDevices(stage);
                        }
                        catch (Exception e) { }
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
            InnerMove(MoverTypes.Y, XYSelector.CurrentValue, false, ReverseY.IsSelected);
        }
        private void YPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.Y, XYSelector.CurrentValue, true, ReverseY.IsSelected);
        }

        private void ZNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.Z, ZSelector.CurrentValue, false, ReverseZ.IsSelected);
        }
        private void ZPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.Z, ZSelector.CurrentValue, true, ReverseZ.IsSelected);
        }

        private void AngleXPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleX, ASelector.CurrentValue, true, ReverseAngleX.IsSelected);
        }

        private void AngleXNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleX, ASelector.CurrentValue, false, ReverseAngleX.IsSelected);
        }

        private void AngleYPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleY, ASelector.CurrentValue, true, ReverseAngleY.IsSelected);
        }

        private void AngleYNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleY, ASelector.CurrentValue, false, ReverseAngleY.IsSelected);
        }

        private void AngleZPositive(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleZ, ASelector.CurrentValue, true, ReverseAngleZ.IsSelected);
        }

        private void AngleZNegative(object sender, RoutedEventArgs e)
        {
            InnerMove(MoverTypes.AngleZ, ASelector.CurrentValue, false, ReverseAngleZ.IsSelected);
        }
    }
}

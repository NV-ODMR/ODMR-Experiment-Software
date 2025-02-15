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
            CreateListener();
        }

        Thread ListenThread = null;

        public void CreateListener()
        {
            ListenThread = new Thread(() =>
              {
                  while (true)
                  {
                      if (MoverPart == PartTypes.Magnnet)
                      {
                          int u = 0;
                      }
                      string xv = "";
                      string yv = "";
                      string zv = "";
                      string axv = "";
                      string ayv = "";
                      string azv = "";
                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.X, MoverPart);
                          xv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.Y, MoverPart);
                          yv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      Dispatcher.Invoke(() =>
                      {
                          XYLocs.Text = "X:  " + xv + "  Y:  " + yv;
                      });

                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.Z, MoverPart);
                          zv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      Dispatcher.Invoke(() =>
                      {
                          ZLocs.Text = "Z:  " + zv;
                      });

                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleX, MoverPart);
                          axv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleY, MoverPart);
                          ayv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      try
                      {
                          var dev = DeviceDispatcher.GetMoverDevice(MoverTypes.AngleZ, MoverPart);
                          azv = dev.Device.Position.ToString();
                      }
                      catch (Exception) { }

                      Dispatcher.Invoke(() =>
                      {
                          AngleLocs.Text = "X:  " + axv + "  Y:  " + ayv + "  Z:  " + azv;
                      });

                      Thread.Sleep(100);
                  }
              });
            ListenThread.Start();
        }

        public void CloseThread()
        {
            if (ListenThread == null) return;
            ListenThread?.Abort();
            while (ListenThread.ThreadState == ThreadState.Running) Thread.Sleep(30);
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
                            stage.Device.MoveStepAndWait((ispositive ? 1 : -1) * step * (isreverse ? 1 : -1), 50, true);
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

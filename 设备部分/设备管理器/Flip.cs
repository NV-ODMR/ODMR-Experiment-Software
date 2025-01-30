using ODMR_Lab.相机;
using ODMR_Lab.设备部分.相机;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    public partial class DeviceDispatcher
    {
        /// <summary>
        /// 自动连接所有相机
        /// </summary>
        /// <param name="connectmessage"></param>
        /// <param name="unconnectmessage"></param>
        private static void ScanFlipDevices(out string connectmessage, out string unconnectmessage)
        {
            connectmessage = "";
            unconnectmessage = "";
            MainWindow.Dev_CameraPage.Flips = FlipMotorInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out List<string> unconnectedDevices).Select(x => x as FlipMotorInfo).ToList();
            foreach (var item in MainWindow.Dev_CameraPage.Flips)
            {
                connectmessage += item.Device.ProductName + "\n";
            }
            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item + "\n";
            }
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                MainWindow.Dev_CameraPage.RefreshPanels();
            });
        }

        /// <summary>
        /// 获取相机设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="showmessagebox"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static FlipMotorInfo TryGetFlipDevice(FlipMotorInfo device)
        {
            try
            {
                if (!MainWindow.Dev_CameraPage.Flips.Contains(device))
                {
                    return null;
                }
                return device;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取相机设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="showmessagebox"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static FlipMotorInfo TryGetFlipDevice(string productName)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_CameraPage.Flips)
                {
                    if (tem.Device.ProductName == productName)
                    {
                        return tem;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取相机设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static FlipMotorInfo TryGetFlipDevice(int index)
        {
            try
            {
                if (index < 0 || index > MainWindow.Dev_CameraPage.Flips.Count - 1)
                {
                    return null;
                }
                return MainWindow.Dev_CameraPage.Flips[index];
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}

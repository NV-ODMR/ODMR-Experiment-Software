using ODMR_Lab.相机;
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
        private static void ScanCameraDevices(out string connectmessage, out string unconnectmessage)
        {
            connectmessage = "";
            unconnectmessage = "";
            MainWindow.Dev_CameraPage.Cameras = CameraInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out List<string> unconnectedDevices).Select(x => x as CameraInfo).ToList();
            foreach (var item in MainWindow.Dev_CameraPage.Cameras)
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
        public static CameraInfo TryGetCameraDevice(CameraInfo device, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                if (!MainWindow.Dev_CameraPage.Cameras.Contains(device))
                {
                    MessageLogger.AddLogger("设备", "未找到对应的相机设备,名称：" + device.Device.ProductName.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (device.IsWriting && mode != OperationMode.Read)
                {
                    MessageLogger.AddLogger("设备", "未能成功获取相机设备" + device.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
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
        public static CameraInfo TryGetCameraDevice(string productName, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_CameraPage.Cameras)
                {
                    if (tem.Device.ProductName == productName)
                    {
                        if (tem.IsWriting && mode != OperationMode.Read)
                        {
                            MessageLogger.AddLogger("设备", "未能成功获取相机设备" + tem.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                            return null;
                        }
                        return tem;
                    }
                }
                MessageLogger.AddLogger("设备", "未找到对应的相机设备" + productName, MessageTypes.Warning, showmessagebox, log);
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
        public static CameraInfo TryGetCameraDevice(int index, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                if (index < 0 || index > MainWindow.Dev_CameraPage.Cameras.Count - 1)
                {
                    MessageLogger.AddLogger("设备", "未找到对应的相机设备，序号：" + index.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (MainWindow.Dev_TemPeraPage.TemperatureControllers[index].IsWriting && mode != OperationMode.Read)
                {
                    MessageLogger.AddLogger("设备", "未能成功获取相机设备" + MainWindow.Dev_TemPeraPage.TemperatureControllers[index].Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                return MainWindow.Dev_CameraPage.Cameras[index];
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}

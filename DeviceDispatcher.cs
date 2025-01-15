using HardWares.温度控制器;
using ODMR_Lab.位移台部分;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.相机;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ODMR_Lab
{
    /// <summary>
    /// 设备管理器
    /// </summary>
    public class DeviceDispatcher
    {
        /// <summary>
        /// 获取温控设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static TemperatureControllerInfo TryGetTemperatureDevice(int index, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                if (index < 0 || index > MainWindow.Dev_TemPeraPage.TemperatureControllers.Count - 1)
                {
                    MessageLogger.AddLogger("未找到对应的温控设备，序号：" + index.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (MainWindow.Dev_TemPeraPage.TemperatureControllers[index].IsWriting)
                {
                    MessageLogger.AddLogger("未能成功获取温控设备" + MainWindow.Dev_TemPeraPage.TemperatureControllers[index].Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                return MainWindow.Dev_TemPeraPage.TemperatureControllers[index];
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取温控设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static TemperatureControllerInfo TryGetTemperatureDevice(TemperatureControllerInfo device, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                if (!MainWindow.Dev_TemPeraPage.TemperatureControllers.Contains(device))
                {
                    MessageLogger.AddLogger("未找到对应的温控设备,名称：" + device.Device.ProductName.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (device.IsWriting && mode != OperationMode.Read)
                {
                    MessageLogger.AddLogger("未能成功获取温控设备" + device.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
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
        /// 获取温控设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static TemperatureControllerInfo TryGetTemperatureDevice(string productName, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_TemPeraPage.TemperatureControllers)
                {
                    if (tem.Device.ProductName == productName)
                    {
                        if (tem.IsWriting && mode != OperationMode.Read)
                        {
                            MessageLogger.AddLogger("未能成功获取温控设备" + tem.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                            return null;
                        }
                        return tem;
                    }
                }
                MessageLogger.AddLogger("未找到对应的温控设备" + productName, MessageTypes.Warning, showmessagebox, log);
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
                    MessageLogger.AddLogger("未找到对应的相机设备,名称：" + device.Device.ProductName.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (device.IsWriting && mode != OperationMode.Read)
                {
                    MessageLogger.AddLogger("未能成功获取相机设备" + device.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
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
                            MessageLogger.AddLogger("未能成功获取相机设备" + tem.Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                            return null;
                        }
                        return tem;
                    }
                }
                MessageLogger.AddLogger("未找到对应的相机设备" + productName, MessageTypes.Warning, showmessagebox, log);
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
                    MessageLogger.AddLogger("未找到对应的相机设备，序号：" + index.ToString(), MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                if (MainWindow.Dev_TemPeraPage.TemperatureControllers[index].IsWriting && mode != OperationMode.Read)
                {
                    MessageLogger.AddLogger("未能成功获取相机设备" + MainWindow.Dev_TemPeraPage.TemperatureControllers[index].Device.ProductName + "，设备正在使用。", MessageTypes.Warning, showmessagebox, log);
                    return null;
                }
                return MainWindow.Dev_CameraPage.Cameras[index];
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="showmessagebox"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static NanoStageInfo TryGetMoverDevice(NanoStageInfo device, OperationMode mode, bool showmessagebox, bool log)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item == device)
                        {
                            if (tem.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
                                return null;
                            }
                            return item;
                        }
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
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="showmessagebox"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static NanoStageInfo TryGetMoverDevice(string productName, OperationMode mode, string axisname, bool showmessagebox, bool log)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.Device.AxisName == axisname && tem.Device.ProductName == productName)
                        {
                            if (tem.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
                                return null;
                            }
                            return item;
                        }
                    }
                }

                MessageLogger.AddLogger("未找到对应的位移台设备" + productName + " " + axisname + "轴", MessageTypes.Warning, showmessagebox, log);
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static NanoStageInfo TryGetMoverDevice(MoverTypes moverType, OperationMode mode, PartTypes part, bool showmessagebox, bool log)
        {
            try
            {
                if (part == PartTypes.None)
                {
                    MessageLogger.AddLogger("无法寻找未分配的位移台设备", MessageTypes.Warning, showmessagebox, log);
                    return null;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.MoverType == moverType && item.PartType == part)
                        {
                            if (tem.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
                                return null;
                            }
                            return item;
                        }
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
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static List<NanoStageInfo> TryGetMoverDevice(OperationMode mode, PartTypes part, bool showmessagebox, bool log)
        {
            List<NanoStageInfo> infos = new List<NanoStageInfo>();
            try
            {
                if (part == PartTypes.None)
                {
                    MessageLogger.AddLogger("无法寻找未分配的位移台设备", MessageTypes.Warning, showmessagebox, log);
                    return infos;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.PartType == part)
                        {
                            if (tem.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
                            }
                            infos.Add(item);
                        }
                    }
                }

                return infos;
            }
            catch (Exception ex)
            {
                return infos;
            }
        }

        /// <summary>
        /// 自动搜索设备
        /// </summary>
        public static string ScanDevices()
        {
            string connectedmessage = "";
            string unconnectmessage = "";
            MainWindow.Dev_CameraPage.Cameras = CameraInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out List<string> unconnectedDevices).Select(x => x as CameraInfo).ToList();
            foreach (var item in MainWindow.Dev_CameraPage.Cameras)
            {
                connectedmessage += item.Device.ProductIdentifier + "\n";
            }
            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item + "\n";
            }
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                MainWindow.Dev_CameraPage.RefreshPanels();
            });

            MainWindow.Dev_TemPeraPage.TemperatureControllers = TemperatureControllerInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out unconnectedDevices).Select(x => x as TemperatureControllerInfo).ToList();
            foreach (var item in MainWindow.Dev_TemPeraPage.TemperatureControllers)
            {
                connectedmessage += item.Device.ProductIdentifier + "\n";
            }
            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item + "\n";
            }
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                MainWindow.Dev_TemPeraPage.RefreshConnectedControllers();
                MainWindow.Dev_TemPeraPage.RefreshChannelBtns();
            });

            MainWindow.Dev_MoversPage.MoverList = NanoMoverInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out unconnectedDevices).Select(x => x as NanoMoverInfo).ToList();
            foreach (var item in MainWindow.Dev_MoversPage.MoverList)
            {
                connectedmessage += item.Device.ProductIdentifier + "\n";
            }
            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item + "\n";
            }
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                MainWindow.Dev_MoversPage.RefreshPanels();
            });

            return "已连接的设备:\n" + (connectedmessage == "" ? "无\n" : connectedmessage + "\n") + "未连接的设备:\n" + (unconnectmessage == "" ? "无\n" : unconnectmessage + "\n");
        }

        public static bool CloseDevicesAndSave()
        {
            bool canclose = true;
            for (int i = 0; i < MainWindow.Dev_CameraPage.Cameras.Count; i++)
            {
                string path = MainWindow.Dev_CameraPage.Cameras[i].CloseDeviceInfoAndSaveParams();
                if (path == "") canclose = false;
            }
            for (int i = 0; i < MainWindow.Dev_TemPeraPage.TemperatureControllers.Count; i++)
            {
                string path = MainWindow.Dev_TemPeraPage.TemperatureControllers[i].CloseDeviceInfoAndSaveParams();
                if (path == "") canclose = false;
            }
            for (int i = 0; i < MainWindow.Dev_MoversPage.MoverList.Count; i++)
            {
                string path = MainWindow.Dev_MoversPage.MoverList[i].CloseDeviceInfoAndSaveParams();
                if (path == "") canclose = false;
            }

            return canclose;
        }
    }

    public enum OperationMode
    {
        Write = 0,
        Read = 1,
        ReadWrite = 2
    }
}

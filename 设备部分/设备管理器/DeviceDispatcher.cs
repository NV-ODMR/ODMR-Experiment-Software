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
    public partial class DeviceDispatcher
    {
        /// <summary>
        /// 自动搜索设备
        /// </summary>
        public static string ScanDevices()
        {
            string connectedmessage = "";
            string unconnectmessage = "";
            //相机
            ScanCameraDevices(out string appendconnected, out string appendunconnected);
            connectedmessage += appendconnected;
            unconnectmessage += appendunconnected;
            //温控
            ScanTemperatureControllerDevices(out appendconnected, out appendunconnected);
            connectedmessage += appendconnected;
            unconnectmessage += appendunconnected;
            //位移台
            ScanMoverDevices(out appendconnected, out appendunconnected);
            connectedmessage += appendconnected;
            unconnectmessage += appendunconnected;
            //源表
            ScanPowerMeterDevices(out appendconnected, out appendunconnected);
            connectedmessage += appendconnected;
            unconnectmessage += appendunconnected;

            return "已连接的设备:\n" + (connectedmessage == "" ? "无\n" : connectedmessage + "\n") + "未连接的设备:\n" + (unconnectmessage == "" ? "无\n" : unconnectmessage + "\n");
        }

        public static bool CloseDevicesAndSave()
        {
            bool canclose = true;
            for (int i = 0; i < MainWindow.Dev_CameraPage.Cameras.Count; i++)
            {
                string path = MainWindow.Dev_CameraPage.Cameras[i].CloseDeviceInfoAndSaveParams(out bool result);
                if (path == "") canclose = false;
            }
            for (int i = 0; i < MainWindow.Dev_TemPeraPage.TemperatureControllers.Count; i++)
            {
                string path = MainWindow.Dev_TemPeraPage.TemperatureControllers[i].CloseDeviceInfoAndSaveParams(out bool result);
                if (path == "") canclose = false;
            }
            for (int i = 0; i < MainWindow.Dev_MoversPage.MoverList.Count; i++)
            {
                string path = MainWindow.Dev_MoversPage.MoverList[i].CloseDeviceInfoAndSaveParams(out bool result);
                if (path == "") canclose = false;
            }
            for (int i = 0; i < MainWindow.Dev_PowerMeterPage.PowerMeterList.Count; i++)
            {
                string path = MainWindow.Dev_PowerMeterPage.PowerMeterList[i].CloseDeviceInfoAndSaveParams(out bool result);
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

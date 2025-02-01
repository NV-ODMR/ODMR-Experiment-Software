using HardWares.温度控制器;
using HardWares.端口基类;
using ODMR_Lab.位移台部分;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.相机;
using ODMR_Lab.设备部分.设备种类枚举;
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
            //翻转镜
            ScanFlipDevices(out appendconnected, out appendunconnected);
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
            for (int i = 0; i < MainWindow.Dev_CameraPage.Flips.Count; i++)
            {
                string path = MainWindow.Dev_CameraPage.Flips[i].CloseDeviceInfoAndSaveParams(out bool result);
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

        /// <summary>
        /// 开始使用多个设备，如果其中有一个设备被占用则释放所有其他已被占用的设备，同时报错
        /// </summary>
        public static void UseDevices(params InfoBase[] devs)
        {
            UseDevices(devs.ToList());
        }

        /// <summary>
        /// 开始使用多个设备，如果其中有一个设备被占用则释放所有其他已被占用的设备，同时报错
        /// </summary>
        public static void UseDevices(List<InfoBase> devs)
        {
            List<InfoBase> dvs = devs.ToList();
            for (int i = 0; i < dvs.Count; i++)
            {
                try
                {
                    dvs[i].BeginUse();
                }
                catch (Exception)
                {
                    for (int j = 0; j < i; j++)
                    {
                        dvs[j].EndUse();
                    }
                    throw new Exception("设备被占用");
                }
            }
        }

        /// <summary>
        /// 开始使用多个设备，如果其中有一个设备被占用则释放所有其他已被占用的设备，同时报错
        /// </summary>
        public static void EndUseDevices(params InfoBase[] devs)
        {
            EndUseDevices(devs.ToList());
        }

        /// <summary>
        /// 开始使用多个设备，如果其中有一个设备被占用则释放所有其他已被占用的设备，同时报错
        /// </summary>
        public static void EndUseDevices(List<InfoBase> devs)
        {
            foreach (var item in devs)
            {
                item.EndUse();
            }
        }

        /// <summary>
        /// 获取设备
        /// </summary>
        /// <returns></returns>
        public static dynamic GetDevices(DeviceTypes type)
        {
            if (type == DeviceTypes.相机)
            {
                return MainWindow.Dev_CameraPage.Cameras;
            }

            if (type == DeviceTypes.温控)
            {
                return MainWindow.Dev_TemPeraPage.TemperatureControllers; 
            }

            if (type == DeviceTypes.磁铁位移台)
            {
                return TryGetMoverDevice(PartTypes.Magnnet); 
            }
            if (type == DeviceTypes.探针位移台)
            {
                return TryGetMoverDevice(PartTypes.Probe); 
            }
            if (type == DeviceTypes.样品位移台)
            {
                return TryGetMoverDevice(PartTypes.Sample);
            }
            if (type == DeviceTypes.微波丝位移台)
            {
                return TryGetMoverDevice(PartTypes.Microwave);
            }

            return new List<InfoBase>();
        }
    }
}

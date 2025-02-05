using HardWares.仪器列表.电动翻转座;
using HardWares.波源;
using HardWares.温度控制器;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using HardWares.纳米位移台;
using ODMR_Lab.位移台部分;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.相机;
using ODMR_Lab.设备部分;
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

            foreach (var item in DevInfos)
            {
                ScanDevice(item, out string appendconnected, out string appendunconnected);
                connectedmessage += appendconnected;
                unconnectmessage += appendunconnected;
            }

            return "已连接的设备:\n" + (connectedmessage == "" ? "无\n" : connectedmessage + "\n") + "未连接的设备:\n" + (unconnectmessage == "" ? "无\n" : unconnectmessage + "\n");
        }

        public static bool CloseDevicesAndSave()
        {
            bool canclose = true;
            foreach (var item in DevInfos)
            {
                canclose = CloseDevice(item);
                if (canclose == false) return canclose;
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

        #region 获取设备
        public static List<InfoBase> GetDevice(DeviceTypes type)
        {
            List<InfoBase> infos = new List<InfoBase>();
            foreach (var item in DevInfos)
            {
                if (item.deviceType == type) return item.GetDevEvent();
            }
            return new List<InfoBase>();
        }
        #endregion
    }
}

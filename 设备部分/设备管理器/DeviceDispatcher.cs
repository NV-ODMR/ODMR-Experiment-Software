using HardWares.仪器列表.电动翻转座;
using HardWares.温度控制器;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using HardWares.端口基类部分.设备信息;
using HardWares.纳米位移台;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ODMR_Lab.设备部分
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

        /// <summary>
        /// 添加设备
        /// </summary>
        public static string AppendDevices()
        {
            string connectedmessage = "";
            string unconnectmessage = "";

            foreach (var item in DevInfos)
            {
                ScanDevice(item, out string appendconnected, out string appendunconnected);
                connectedmessage += appendconnected;
                unconnectmessage += appendunconnected;
            }

            return "新增的设备:\n" + (connectedmessage == "" ? "无\n" : connectedmessage + "\n");
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
            DeviceTypes origitype = type;
            foreach (var item in DevInfos)
            {
                if (type == DeviceTypes.微波位移台 || type == DeviceTypes.探针位移台 || type == DeviceTypes.样品位移台 || type == DeviceTypes.磁铁位移台 || type == DeviceTypes.镜头位移台 || type == DeviceTypes.AFM扫描台)
                    type = DeviceTypes.位移台;
                if (item.deviceType == type)
                {
                    if (type == DeviceTypes.信号发生器通道)
                    {
                        var temp = item.GetDevEvent();
                        foreach (var dev in temp)
                        {
                            infos.AddRange((dev as SignalGeneratorInfo).Channels);
                        }
                    }
                    else
                    {
                        infos = item.GetDevEvent();
                    }
                    PartTypes part = PartTypes.None;
                    if (origitype == DeviceTypes.微波位移台)
                        part = PartTypes.Microwave;
                    if (origitype == DeviceTypes.探针位移台)
                        part = PartTypes.Probe;
                    if (origitype == DeviceTypes.样品位移台)
                        part = PartTypes.Sample;
                    if (origitype == DeviceTypes.磁铁位移台)
                        part = PartTypes.Magnnet;
                    if (origitype == DeviceTypes.镜头位移台)
                        part = PartTypes.Len;
                    if (origitype == DeviceTypes.AFM扫描台)
                        part = PartTypes.Scanner;
                    if (part != PartTypes.None)
                    {
                        List<InfoBase> stageinfos = new List<InfoBase>();
                        foreach (var stage in infos)
                        {
                            stageinfos.AddRange((stage as NanoMoverInfo).Stages.Where(x => x.PartType == part));
                        }
                        infos = stageinfos;
                    }
                }
            }
            return infos;
        }

        /// <summary>
        /// 获取设备，找不到则返回null
        /// </summary>
        /// <param name="type"></param>
        /// <param name="devicedescription"></param>
        /// <returns></returns>
        public static InfoBase GetDevice(DeviceTypes type, string devicedescription)
        {
            List<InfoBase> infos = GetDevice(type);
            var info = infos.Where(x => x.GetDeviceDescription() == devicedescription);
            if (infos.Where(x => x.GetDeviceDescription() == devicedescription).Count() == 0) return null;
            return info.ElementAt(0);
        }
        #endregion
    }
}

using HardWares.纳米位移台;
using HardWares.温度控制器;
using HardWares.相机_CCD_;
using HardWares.仪器列表.电动翻转座;
using ODMR_Lab.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.相机;
using ODMR_Lab.温度监测部分;
using ODMR_Lab.设备部分.相机;
using System.IO;
using ODMR_Lab.射频源_锁相放大器;
using ODMR_Lab.设备部分;
using System.Reflection;
using HardWares.端口基类部分.设备信息;

namespace ODMR_Lab
{
    public partial class DeviceDispatcher
    {
        private class DeviceDispatcherInfoBase
        {
            public DeviceTypes deviceType { get; set; }
            /// <summary>
            /// 所属页面
            /// </summary>
            public DevicePageBase Page { get; set; } = null;

            public Type DeviceInfoType = null;

            /// <summary>
            /// 设置设备事件
            /// </summary>
            public Action<List<InfoBase>> SetDevEvent = null;

            public delegate List<InfoBase> GetDevHandler();

            public GetDevHandler GetDevEvent = null;
        }
        private class DeviceDispatcherInfo<T> : DeviceDispatcherInfoBase
            where T : InfoBase
        {

            public DeviceDispatcherInfo(DeviceTypes type, DevicePageBase page, List<T> devinfos)
            {
                deviceType = type;
                Page = page;
                DeviceInfoType = typeof(T);
            }

        }

        private static void ScanDevice(DeviceDispatcherInfoBase info, out string connectmessage, out string unconnectmessage)
        {
            connectmessage = "";
            unconnectmessage = "";
            List<KeyValuePair<Type, DeviceInfoBase>> connectedDevices = new List<KeyValuePair<Type, DeviceInfoBase>>();
            List<KeyValuePair<Type, DeviceInfoBase>> unconnectedDevices = new List<KeyValuePair<Type, DeviceInfoBase>>();

            MethodInfo inf = info.DeviceInfoType.BaseType.GetMethod("ConnectAllAndLoadParams");

            object[] param = new object[] { Path.Combine(Environment.CurrentDirectory, "DevParamDir"), null, null };

            object result = inf.Invoke(null, param);

            info.SetDevEvent(result as List<InfoBase>);

            connectedDevices = (List<KeyValuePair<Type, DeviceInfoBase>>)param[1];
            unconnectedDevices = (List<KeyValuePair<Type, DeviceInfoBase>>)param[2];


            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                info.Page.RefreshPanels();
            });

            foreach (var item in connectedDevices)
            {
                connectmessage += item.Value.DeviceName + "\n";
            }

            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item.Value.DeviceName + "\n";
            }
        }

        private static bool CloseDevice(DeviceDispatcherInfoBase info)
        {
            List<InfoBase> infos = info.GetDevEvent();
            foreach (var item in infos)
            {
                MethodInfo inf = item.GetType().GetMethod("CloseDeviceInfoAndSaveParams");
                string path = inf.Invoke(item, new object[] { null }) as string;
                if (path == "") return false;
            }
            return true;
        }

        private static List<DeviceDispatcherInfoBase> DevInfos { get; set; } = new List<DeviceDispatcherInfoBase>()
        {
            //温控
            new DeviceDispatcherInfo<TemperatureControllerInfo>(DeviceTypes.温控,MainWindow.Dev_TemPeraPage,MainWindow.Dev_TemPeraPage.TemperatureControllers)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_TemPeraPage.TemperatureControllers.AddRange(x.Select(v=>v as TemperatureControllerInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_TemPeraPage.TemperatureControllers.Select(x=>x as InfoBase).ToList(); })
            },

             //位移台
            new DeviceDispatcherInfo<NanoMoverInfo>(DeviceTypes.位移台,MainWindow.Dev_MoversPage,MainWindow.Dev_MoversPage.MoverList)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_MoversPage.MoverList.AddRange(x.Select(v=>v as NanoMoverInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_MoversPage.MoverList.Select(x=>x as InfoBase).ToList(); })
            },

             //射频源
            new DeviceDispatcherInfo<RFSourceInfo>(DeviceTypes.射频源,MainWindow.Dev_RFSource_LockInPage,MainWindow.Dev_RFSource_LockInPage.RFSources)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_RFSource_LockInPage.RFSources.AddRange(x.Select(v=>v as RFSourceInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_RFSource_LockInPage.RFSources.Select(x=>x as InfoBase).ToList(); })
            },

            //相机
            new DeviceDispatcherInfo<CameraInfo>(DeviceTypes.相机,MainWindow.Dev_CameraPage,MainWindow.Dev_CameraPage.Cameras)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_CameraPage.Cameras.AddRange(x.Select(v=>v as CameraInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_CameraPage.Cameras.Select(x=>x as InfoBase).ToList(); })
            },

            //翻转镜
            new DeviceDispatcherInfo<FlipMotorInfo>(DeviceTypes.翻转镜,MainWindow.Dev_CameraPage,MainWindow.Dev_CameraPage.Flips)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_CameraPage.Flips.AddRange(x.Select(v=>v as FlipMotorInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_CameraPage.Flips.Select(x=>x as InfoBase).ToList(); })
            },

            //源表
            new DeviceDispatcherInfo<PowerMeterInfo>(DeviceTypes.源表,MainWindow.Dev_PowerMeterPage,MainWindow.Dev_PowerMeterPage.PowerMeterList)
            {
                SetDevEvent=new Action<List<InfoBase>>(x=>MainWindow.Dev_PowerMeterPage.PowerMeterList.AddRange(x.Select(v=>v as PowerMeterInfo).ToList())),
                GetDevEvent=new DeviceDispatcherInfoBase.GetDevHandler(()=>{return MainWindow.Dev_PowerMeterPage.PowerMeterList.Select(x=>x as InfoBase).ToList(); })
            },
        };

    }
}

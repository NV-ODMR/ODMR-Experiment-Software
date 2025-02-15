using CodeHelper;
using HardWares;
using HardWares.相机_CCD_;
using HardWares.端口基类;
using HardWares.端口基类部分.设备信息;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    public abstract class InfoBase
    {
        public bool IsWriting { get; private set; } = false;

        protected object SourceDevice { get; set; } = null;


        private object lockobj = new object();

        /// <summary>
        /// 创建DeviceInfo的具体方法
        /// </summary>
        public abstract void CreateDeviceInfoBehaviour();

        /// <summary>
        /// 使用设备,向设备写信息，当设备处于使用状态时其他无关操作会导致报错
        /// </summary>
        public void BeginUse()
        {
            lock (lockobj)
            {
                if (IsWriting)
                {
                    throw new Exception("未能成功获取设备," + "设备正在使用。");
                }
                IsWriting = true;
                return;
            }
        }

        /// <summary>
        /// 停止向设备写信息，当设备处于使用状态时其他无关操作会导致报错
        /// </summary>
        public void EndUse()
        {
            lock (lockobj)
            {
                IsWriting = false;
            }
        }

        /// <summary>
        /// 获取设备描述
        /// </summary>
        /// <returns></returns>
        public abstract string GetDeviceDescription();
    }

    public abstract class DeviceInfoBase<T> : InfoBase
        where T : PortObject
    {
        /// <summary>
        /// 设备名
        /// </summary>
        public new T Device
        {
            get
            {
                return (T)SourceDevice;
            }
            set
            {
                SourceDevice = value;
            }
        }

        public DeviceInfoBase ConnectInfo { get; set; } = null;

        protected abstract void CloseFileAction(FileObject obj);

        protected abstract void AutoConnectedAction(FileObject file);

        /// <summary>
        /// 设备关闭事件
        /// </summary>
        public event Action CloseDeviceEvent = null;

        /// <summary>
        /// 关闭设备，保存参数，返回参数文件路径
        /// </summary>
        public string CloseDeviceInfoAndSaveParams(out bool IsSucceedClosed)
        {
            if (!IsWriting)
            {
                PortObject dev = Device;
                CloseDeviceEvent?.Invoke();
                FileObject obj = new FileObject();
                obj = dev.GenerateParamsFile();
                dev.Dispose();
                CloseFileAction(obj);
                obj.Descriptions.Add("FileType", "DeviceParamsFile");
                obj.Descriptions.Add("PortType", Enum.GetName(dev.PortType.GetType(), dev.PortType));
                obj.Descriptions.Add("DeviceType", dev.GetType().ToString());
                obj.Descriptions.Add("DeviceInfoType", GetType().ToString());
                obj.Descriptions.Add("DevName", ConnectInfo.DeviceName);
                if (ConnectInfo is USBDeviceInfo)
                {
                    obj.Descriptions.Add("USBConnectName", (ConnectInfo as USBDeviceInfo).USBIdentification);
                }
                if (ConnectInfo is COMDeviceInfo)
                {
                    obj.Descriptions.Add("COMName", (ConnectInfo as COMDeviceInfo).COMName);
                    obj.Descriptions.Add("BaudRate", (ConnectInfo as COMDeviceInfo).BaudRate.ToString());
                }
                if (ConnectInfo is TCPIPDeviceInfo)
                {
                    obj.Descriptions.Add("TCPIPAddress", (ConnectInfo as TCPIPDeviceInfo).IPAddress);
                    obj.Descriptions.Add("TCPIPPort", (ConnectInfo as TCPIPDeviceInfo).Port.ToString());
                }
                if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "DevParamDir")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "DevParamDir"));
                }

                string rBuilder = Path.Combine(dev.ProductIdentifier + "_" + dev.ProductName);
                foreach (char rinvalidChar in Path.GetInvalidFileNameChars())
                    rBuilder.Replace(rinvalidChar.ToString(), "");

                string path = Path.Combine(Path.Combine(Environment.CurrentDirectory, "DevParamDir", rBuilder.ToString().Trim()));
                obj.SaveToFile(path);
                IsSucceedClosed = true;
                return path + ".userdat";
            }
            else
            {
                MessageLogger.AddLogger("设备", "此设备正在被使用，无法关闭", MessageTypes.Warning, true, true);
                IsSucceedClosed = false;
                return "";
            }
        }

        /// <summary>
        /// 尝试打开所有设备，导入参数
        /// </summary>
        public static List<InfoBase> ConnectAllAndLoadParams(string ParamFolder, out List<KeyValuePair<Type, DeviceInfoBase>> connectedDeviceinfo, out List<KeyValuePair<Type, DeviceInfoBase>> unconnectedDeviceinfo)
        {
            if (!Directory.Exists(ParamFolder))
            {
                Directory.CreateDirectory(ParamFolder);
            }
            connectedDeviceinfo = new List<KeyValuePair<Type, DeviceInfoBase>>();
            unconnectedDeviceinfo = new List<KeyValuePair<Type, DeviceInfoBase>>();
            List<InfoBase> ConnectDevices = new List<InfoBase>();
            DirectoryInfo info = new DirectoryInfo(ParamFolder);
            FileInfo[] files = info.GetFiles();
            foreach (var item in files)
            {
                if (item.Name.Contains(".userdat"))
                {
                    try
                    {
                        FileObject obj = FileObject.ReadFromFile(item.FullName);

                        if (obj.Descriptions.Values.Contains("DeviceParamsFile"))
                        {
                            //文件是设备参数的文件类型
                            Dictionary<string, Type> devicetypes = PortObject.GetDeviceNamesWithSameBase(typeof(T));
                            foreach (var device in devicetypes)
                            {
                                if (device.Value.ToString() == obj.Descriptions["DeviceType"])
                                {
                                    //找到确定的设备类型
                                    //打开设备
                                    PortType port = (PortType)Enum.Parse(typeof(PortType), obj.Descriptions["PortType"]);
                                    var deviceinstance = Activator.CreateInstance(device.Value);
                                    bool connectresult = false;

                                    DeviceInfoBase connectinfo = null;

                                    if (port == PortType.USB)
                                    {
                                        connectinfo = new USBDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["USBConnectName"]);
                                        connectresult = (deviceinstance as WinUSBOuterInterface).ConnectUSB(connectinfo as USBDeviceInfo, out Exception exc);
                                    }
                                    if (port == PortType.COM)
                                    {
                                        connectinfo = new COMDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["COMName"], int.Parse(obj.Descriptions["BaudRate"]));
                                        connectresult = (deviceinstance as COMOuterInterface).ConnectCOM(connectinfo as COMDeviceInfo, out Exception exc);
                                    }
                                    if (port == PortType.TCPIP)
                                    {
                                        connectinfo = new TCPIPDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["TCPIPAddress"], int.Parse(obj.Descriptions["TCPIPPort"]));
                                        connectresult = (deviceinstance as TCPIPOuterInterface).ConnectTCPIP(connectinfo as TCPIPDeviceInfo, out Exception exc);
                                    }

                                    if (connectresult == false)
                                    {
                                        unconnectedDeviceinfo.Add(new KeyValuePair<Type, DeviceInfoBase>(device.Value, connectinfo));
                                        continue;
                                    }
                                    else
                                    {
                                        connectedDeviceinfo.Add(new KeyValuePair<Type, DeviceInfoBase>(device.Value, connectinfo));
                                    }

                                    Type infoType = Type.GetType(obj.Descriptions["DeviceInfoType"]);
                                    DeviceInfoBase<T> dev = Activator.CreateInstance(infoType) as DeviceInfoBase<T>;
                                    dev.SourceDevice = deviceinstance as PortObject;
                                    dev.ConnectInfo = connectinfo;
                                    dev.CreateDeviceInfoBehaviour();
                                    dev.AutoConnectedAction(obj);

                                    ConnectDevices.Add(dev);
                                    if (obj.DataCount() != 0)
                                    {
                                        dev.Device.LoadParams(obj);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            return ConnectDevices;
        }

        /// <summary>
        /// 按照配置文件打开指定设备并导入参数
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static DeviceInfoBase<T> OpenAndLoadParams(string filepath, Type deviceType)
        {
            FileObject obj = FileObject.ReadFromFile(filepath);
            //找到确定的设备类型
            //打开设备
            PortType port = (PortType)Enum.Parse(typeof(PortType), obj.Descriptions["PortType"]);
            var deviceinstance = Activator.CreateInstance(deviceType);
            bool connectresult = false;

            DeviceInfoBase connectinfo = null;

            if (port == PortType.USB)
            {
                connectinfo = new USBDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["USBConnectName"]);
                connectresult = (deviceinstance as WinUSBOuterInterface).ConnectUSB(connectinfo as USBDeviceInfo, out Exception exc);
            }
            if (port == PortType.COM)
            {
                connectinfo = new COMDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["COMName"], int.Parse(obj.Descriptions["BaudRate"]));
                connectresult = (deviceinstance as COMOuterInterface).ConnectCOM(connectinfo as COMDeviceInfo, out Exception exc);
            }
            if (port == PortType.TCPIP)
            {
                connectinfo = new TCPIPDeviceInfo(obj.Descriptions["DevName"], obj.Descriptions["TCPIPAddress"], int.Parse(obj.Descriptions["TCPIPPort"]));
                connectresult = (deviceinstance as TCPIPOuterInterface).ConnectTCPIP(connectinfo as TCPIPDeviceInfo, out Exception exc);
            }

            if (connectresult == false)
            {
                throw new Exception("设备连接失败");
            }

            Type infoType = Type.GetType(obj.Descriptions["DeviceInfoType"]);
            DeviceInfoBase<T> res = Activator.CreateInstance(infoType) as DeviceInfoBase<T>;
            res.SourceDevice = deviceinstance as PortObject;
            res.ConnectInfo = connectinfo;
            res.CreateDeviceInfoBehaviour();
            res.AutoConnectedAction(obj);

            if (obj.DataCount() != 0)
            {
                res.Device.LoadParams(obj);
            }

            return res;
        }
    }

    public abstract class DeviceElementInfoBase<T> : InfoBase
    {
        /// <summary>
        /// 设备名
        /// </summary>
        public new T Device
        {
            get
            {
                return (T)SourceDevice;
            }
            set
            {
                SourceDevice = value;
            }
        }
    }
}

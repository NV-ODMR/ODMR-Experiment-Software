using ODMR_Lab.位移台部分;
using ODMR_Lab.温度监测部分;
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
        /// 自动连接所有位移台
        /// </summary>
        /// <param name="connectmessage"></param>
        /// <param name="unconnectmessage"></param>
        private static void ScanMoverDevices(out string connectmessage, out string unconnectmessage)
        {
            connectmessage = "";
            unconnectmessage = "";

            MainWindow.Dev_MoversPage.MoverList = NanoMoverInfo.ConnectAllAndLoadParams(Path.Combine(Environment.CurrentDirectory, "DevParamDir"), out List<string> unconnectedDevices).Select(x => x as NanoMoverInfo).ToList();
            foreach (var item in MainWindow.Dev_MoversPage.MoverList)
            {
                connectmessage += item.Device.ProductName + "\n";
            }
            foreach (var item in unconnectedDevices)
            {
                unconnectmessage += item + "\n";
            }
            MainWindow.Handle.Dispatcher.Invoke(() =>
            {
                MainWindow.Dev_MoversPage.RefreshPanels();
            });
        }


        /// <summary>
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="showmessagebox"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static NanoStageInfo TryGetMoverDevice(NanoStageInfo device, OperationMode mode, bool showmessagebox = false, bool log = true)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item == device)
                        {
                            if (item.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("设备", "未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
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
        public static NanoStageInfo TryGetMoverDevice(string productName, OperationMode mode, string axisname, bool showmessagebox = false, bool log = true)
        {
            try
            {
                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.Device.AxisName == axisname && tem.Device.ProductName == productName)
                        {
                            if (item.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("设备", "未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
                                return null;
                            }
                            return item;
                        }
                    }
                }

                MessageLogger.AddLogger("设备", "未找到对应的位移台设备" + productName + " " + axisname + "轴", MessageTypes.Warning, showmessagebox, log);
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
        public static NanoStageInfo TryGetMoverDevice(MoverTypes moverType, OperationMode mode, PartTypes part, bool showmessagebox = false, bool log = true)
        {
            try
            {
                if (part == PartTypes.None)
                {
                    MessageLogger.AddLogger("设备", "无法寻找未分配的位移台设备", MessageTypes.Warning, showmessagebox, log);
                    return null;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.MoverType == moverType && item.PartType == part)
                        {
                            if (item.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("设备", "未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
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
        public static List<NanoStageInfo> TryGetMoverDevice(OperationMode mode, PartTypes part, bool showmessagebox = false, bool log = true)
        {
            List<NanoStageInfo> infos = new List<NanoStageInfo>();
            try
            {
                if (part == PartTypes.None)
                {
                    MessageLogger.AddLogger("设备", "无法寻找未分配的位移台设备", MessageTypes.Warning, showmessagebox, log);
                    return infos;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.PartType == part)
                        {
                            if (item.IsWriting && mode != OperationMode.Read)
                            {
                                MessageLogger.AddLogger("设备", "未能成功获取位移台设备" + tem.Device.ProductName + "的" + item.Device.AxisName + "轴,轴正在使用。", MessageTypes.Warning, showmessagebox, log);
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
    }
}

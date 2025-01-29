using HardWares.纳米位移台;
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
        public static NanoStageInfo TryGetMoverDevice(MoverTypes moverType, OperationMode mode, PartTypes part, bool showmessagebox = false, bool log = true)
        {
            try
            {
                if (part == PartTypes.None)
                {
                    return null;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.MoverType == moverType && item.PartType == part)
                        {
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
                    return infos;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.PartType == part)
                        {
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

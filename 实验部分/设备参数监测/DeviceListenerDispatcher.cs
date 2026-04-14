using CodeHelper;
using Controls;
using HardWares;
using HardWares.端口基类;
using HardWares.端口基类部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.温度监测;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using ComboBox = Controls.ComboBox;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.设备参数监测
{
    /// <summary>
    /// 设备参数监控类
    /// </summary>
    public class DeviceListenerDispatcher
    {

        public DisplayPage ParentPage = null;
        /// <summary>
        /// 需要监控的设备列表
        /// </summary>
        public List<DeviceListenInfo> DeviceParameters = new List<DeviceListenInfo>();

        private int validategap = 100;
        /// <summary>
        /// 参数更新周期（毫秒，最小值30ms）
        /// </summary>
        public int ValidateGap
        {
            get { return validategap; }
            set
            {
                if (value < 30) ValidateGap = 30;
                else
                {
                    validategap = value;
                }
            }
        }

        public DeviceListenerDispatcher(DisplayPage parent, string loadfilepath = "")
        {
            ParentPage = parent;
            if (loadfilepath != "") ReadFromFile(loadfilepath);
        }

        /// <summary>
        /// 参数更新事件，参数为当前监控的设备参数列表（包含设备信息、时间、参数值）
        /// </summary>
        /// <returns></returns>
        public event Action<List<Tuple<DeviceListenInfo, DateTime, double>>> ParamValidatedAction = null;

        Thread listehthread = null;

        Thread PlotThread = null;

        public bool IsLoopEnd = false;

        private object Lock = new object();

        /// <summary>
        /// 开始监控
        /// </summary>
        public void StartListen()
        {
            if (listehthread == null)
            {
                listehthread = new Thread(() =>
                {
                    while (true)
                    {
                        IsLoopEnd = false;
                        List<Tuple<DeviceListenInfo, DateTime, double>> values = new List<Tuple<DeviceListenInfo, DateTime, double>>();
                        lock (Lock)
                        {
                            foreach (var item in DeviceParameters)
                            {
                                //item.LastValue = double.NaN;
                                if (item.IsSample == false) { item.ErrorMessage = ""; continue; }
                                try
                                {
                                    if (!PortObject.IsDeviceConnected(item.Device))
                                    {
                                        item.ErrorMessage = "设备未连接";
                                    }
                                    else
                                    {
                                        item.LastValue = ValueConverter(item.TargetParameter.ReadValue());
                                        item.ErrorMessage = "";
                                        item.AddData(DateTime.Now, item.LastValue);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    item.ErrorMessage = ex.Message;
                                }
                            }
                        }
                        IsLoopEnd = true;
                        Thread.Sleep(ValidateGap);
                    }
                });
                listehthread.IsBackground = true;
                listehthread.Start();

                PlotThread = new Thread(() =>
                {
                    while (true)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (ParentPage.IsFixedTime.IsSelected)
                            {
                                ParentPage.UpdatePlotWithFixedTimeLength();
                            }
                            else
                                ParentPage.Chart.RefreshPlotWithAutoScaleY();
                            //更新数据面板
                            foreach (var item in DeviceParameters)
                            {
                                item.DeviceNumberBar.ParamValue.Text = item.LastValue.ToString("G7");
                                item.ValidateErrorDisplay();
                            }
                        });
                        Thread.Sleep(150);
                    }
                });
                PlotThread.IsBackground = true;
                PlotThread.Start();
            }
        }

        private double ValueConverter(dynamic value)
        {
            if (value is double || value is float || value is int || value is long)
            {
                return (double)value;
            }
            if (value is bool)
            {
                return (bool)value ? 1 : 0;
            }
            return double.NaN;
        }

        public void StopListen()
        {
            try
            {
                listehthread.Abort();
                while (listehthread.IsAlive)
                {
                    Thread.Sleep(50);
                }
                PlotThread.Abort();
                while (PlotThread.IsAlive)
                {
                    Thread.Sleep(50);
                }
            }
            catch (Exception)
            {
            }
        }

        public void AddListenInfo(DeviceListenInfo info)
        {
            lock (Lock)
            {
                DeviceParameters.Add(info);
            }
        }

        public void RemoveListenInfo(DeviceListenInfo info)
        {
            lock (Lock)
            {
                DeviceParameters.Remove(info);
            }
        }

        public DeviceListenInfo GetListenInfo(int index)
        {
            return DeviceParameters[index];
        }

        #region 文件操作
        public void ReadFromFile(string filepath)
        {
            FileObject obj = FileObject.ReadFromFile(filepath);

            var devproductnames = obj.ExtractString("ProductName");
            var devproductidentifiers = obj.ExtractString("ProductIdentifier");
            var haschannel = obj.ExtractBoolean("HasChannel");
            var channelnames = obj.ExtractString("ChannelName");
            var parameters = obj.ExtractString("ParameterName");
            var parameterdescs = obj.ExtractString("ParameterDescription");
            var ChannelColor = obj.ExtractString("ChannelColor");

            DeviceParameters.Clear();
            for (int i = 0; i < devproductnames.Count; i++)
            {
                try
                {
                    DeviceListenInfo info = new DeviceListenInfo(ParentPage, devproductnames[i], devproductidentifiers[i], haschannel[i] ? channelnames[i] : "", parameters[i], parameterdescs[i]);
                    ParentPage.AppendInfoCommands(info);
                    DeviceParameters.Add(info);
                    info.DisplayColor = (Color)ColorConverter.ConvertFromString(ChannelColor[i]);

                    PortObject dev = PortObject.FindDevice(devproductidentifiers[i], devproductnames[i]);
                    if (dev == null) continue;
                    PortElement channel = null;
                    if (haschannel[i])
                    {
                        channel = (dev as ElementPortObject).Channels.Where((x) => x.ChannelName == channelnames[i]).ElementAt(0);
                    }
                    Parameter p = null;
                    if (channel != null)
                    {
                        p = channel.AvailableParameterNames().Where((x) => x.ParameterName == parameters[i]).ElementAt(0);
                    }
                    else
                    {
                        p = dev.AvailableParameterNames().Where((x) => x.ParameterName == parameters[i]).ElementAt(0);
                    }

                    info.ValidateDev(dev, channel, p);
                }
                catch (Exception)
                {

                }
            }
        }

        public void WriteToFile(string filepath)
        {
            FileObject obj = new FileObject();
            var devproductnames = DeviceParameters.Select(p => p.Device.ProductName).ToList();
            var devproductidentifiers = DeviceParameters.Select(p => p.Device.ProductIdentifier).ToList();

            var haschannel = DeviceParameters.Select(p => p.Channel == null ? false : true).ToList();
            var channelnames = DeviceParameters.Select(p => p.Channel == null ? "NoChannel" : p.Channel.ChannelName).ToList();

            var parameters = DeviceParameters.Select(p => p.TargetParameter.ParameterName).ToList();
            var parameterdescs = DeviceParameters.Select(p => p.TargetParameter.Description).ToList();

            var ChannelColor = DeviceParameters.Select(p => p.DisplayColor.ToString()).ToList();


            obj.WriteStringData("ProductName", devproductnames);
            obj.WriteStringData("ProductIdentifier", devproductidentifiers);
            obj.WriteBooleanData("HasChannel", haschannel);
            obj.WriteStringData("ChannelName", channelnames);
            obj.WriteStringData("ParameterName", parameters);
            obj.WriteStringData("ChannelColor", ChannelColor);
            obj.WriteStringData("ParameterDescription", parameterdescs);

            obj.SaveToFile(filepath);
        }
        #endregion
    }
}

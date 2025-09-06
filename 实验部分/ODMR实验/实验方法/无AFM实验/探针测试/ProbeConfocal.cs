using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.探针测试
{
    public class Confocal : Scan2DExpBase<NanoStageInfo, NanoStageInfo>
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = true;

        public override string ODMRExperimentName { get; set; } = "阵列探针测试(共聚焦,AutoTrace,CW)";
        public override string ODMRExperimentGroupName { get; set; } = "探针测试";

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("采样率(Hz)",60,"SampleRate"),
            new Param<int>("位移台等待时间(ms)",0,"MoverWaitingTime"),
            new Param<int>("自动测量Autotrace次数",5,"AutotraceNumber")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头X","","LenX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Y","","LenY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.镜头位移台,new Param<string>("镜头Z","","LenZ")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.光子计数器,new Param<string>("APD","","APD")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("激光触发源","","PB")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.PulseBlaster,new Param<string>("APD Trace 触发源","","TraceSource")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
                new AutoTrace(),
                new TotalCW()
            };
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            var btns = new List<KeyValuePair<string, Action>>();
            btns.Add(new KeyValuePair<string, Action>("AutoTrace", DoAutoTrace));
            btns.Add(new KeyValuePair<string, Action>("CW", DoCW));
            btns.Add(new KeyValuePair<string, Action>("移动到选定位置", MoveToCursor));
            btns.Add(new KeyValuePair<string, Action>("多点自动测量", DoMultiMeasure));
            return btns;
        }

        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override string CreateThreadState(NanoStageInfo dev1, NanoStageInfo dev2, Point loc)
        {
            return "共聚焦扫描 X: " + Math.Round(loc.X, 5).ToString() + " Y: " + Math.Round(loc.Y, 5).ToString();
        }

        protected override List<object> FirstScanEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        protected override List<object> EndScanNewLineEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        protected override List<object> StartScanNewLineEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            device1.Device.MoveToAndWait(loc.X, 3000);
            device2.Device.MoveToAndWait(loc.Y, 3000);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        protected override List<object> ScanEvent(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            int waittime = GetInputParamValueByName("MoverWaitingTime");
            device1.Device.MoveToAndWait(loc.X, waittime);
            device2.Device.MoveToAndWait(loc.Y, waittime);
            return ScanCore(device1, device2, range, loc, inputParams);
        }

        private List<object> ScanCore(NanoStageInfo device1, NanoStageInfo device2, D2ScanRangeBase range, Point loc, List<object> inputParams)
        {
            ConfocalAPDSample a = new ConfocalAPDSample();
            var res = a.CoreMethod(new List<object>() { GetInputParamValueByName("SampleRate") }, GetDeviceByName("APD"));
            int count = (int)(double)res[0];
            //画图
            var chartdata = Get2DChartData("计数率(cps)", "共聚焦扫描结果");
            chartdata.Data.SetValue(range.GetNearestXIndex(loc.X), range.GetNearestYIndex(loc.Y), count);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }


        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }
        public override NanoStageInfo GetScanSource1()
        {
            return GetDeviceByName("LenX") as NanoStageInfo;
        }

        public override NanoStageInfo GetScanSource2()
        {
            return GetDeviceByName("LenY") as NanoStageInfo;
        }

        protected override void Preview2DScanEventWithoutAFM()
        {
            //打开激光
            LaserOn lon = new LaserOn();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            lon.CoreMethod(new List<object>() { 1.0 }, pb);
            //设置触发源
            PulseBlasterInfo trace = GetDeviceByName("TraceSource") as PulseBlasterInfo;
            trace.Device.PulseFrequency = GetInputParamValueByName("SampleRate");
            trace.Device.Start();
            //打开APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.StartContinusSample();
            //创建数据集
            var r1 = GetScanRange();
            //新建数据集
            D2ChartDatas = new List<ChartData2D>()
            {
                new ChartData2D(new FormattedDataSeries2D(r1.XCount,r1.XLo,r1.XHi,r1.YCount,r1.YLo,r1.YHi){
                    XName="X轴位置(μm)",YName="Y轴位置(μm)",ZName="计数率(cps)"}){ GroupName="共聚焦扫描结果"}
            };
            UpdatePlotChart();
            //选中数据集
            Show2DChartData("共聚焦扫描结果", "X轴位置(μm)", "Y轴位置(μm)", "计数率(cps)");
        }

        protected override void After2DScanEventWithoutAFM()
        {
            //关闭激光
            LaserOff loff = new LaserOff();
            PulseBlasterInfo pb = GetDeviceByName("PB") as PulseBlasterInfo;
            loff.CoreMethod(new List<object>() { }, pb);
            //打开APD
            APDInfo apd = GetDeviceByName("APD") as APDInfo;
            apd.EndContinusSample();
            //关闭APD触发源
            (GetDeviceByName("TraceSource") as PulseBlasterInfo).Device.End();
        }

        #region 按钮操作部分
        /// <summary>
        /// AutoTrace
        /// </summary>
        private void DoAutoTrace()
        {
            try
            {
                var exp = RunSubExperimentBlock(0, true);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageWindow.ShowTipWindow("AutoTrace出现问题:" + ex.Message, Window.GetWindow(ParentPage));
                });
            }
        }

        /// <summary>
        /// AutoTrace
        /// </summary>
        private void DoCW()
        {
            try
            {
                var exp = RunSubExperimentBlock(1, true);
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageWindow.ShowTipWindow("CW扫描出现问题:" + ex.Message, Window.GetWindow(ParentPage));
                });
            }
        }

        /// <summary>
        /// 移动到光标位置
        /// </summary>
        private void MoveToCursor()
        {
            Point p = ParentPage.Chart2D.GetSelectedCursor();
            MoveToCursor(p);
        }

        private void MoveToCursor(Point p)
        {
            //刷新可用设备
            GetDevices();
            var lx = GetDeviceByName("LenX") as NanoStageInfo;
            var ly = GetDeviceByName("LenY") as NanoStageInfo;
            try
            {
                DeviceDispatcher.UseDevices(lx, ly);
                if (double.IsNaN(p.X) || double.IsNaN(p.Y)) return;
                lx.Device.MoveToAndWait(p.X, 3000);
                ly.Device.MoveToAndWait(p.Y, 3000);
                DeviceDispatcher.EndUseDevices(lx, ly);
                SetExpState("移动完成,当前位置: X: " + Math.Round(p.X, 5).ToString() + " Y: " + Math.Round(p.Y, 5).ToString());
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    MessageWindow.ShowTipWindow("设备被占用", Window.GetWindow(ParentPage));
                });
            }
        }

        //多点测量
        private void DoMultiMeasure()
        {
            var cursors = ParentPage.Chart2D.GetCursors();
            bool isContinue = true;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (MessageBoxResult.Yes != MessageWindow.ShowMessageBox("提示", "选中的测量点数:" + cursors.Count.ToString() + "\n确定要执行扫描吗?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)))
                {
                    isContinue = false;
                }
            });
            if (!isContinue) return;
            var ylocs = cursors.Select(x => x.Y).ToHashSet().ToList();
            ylocs.Sort();
            List<Point> newps = new List<Point>();
            foreach (var item in ylocs)
            {
                var xlocs = cursors.Where(x => x.Y == item).ToList();
                xlocs.Sort((v1, v2) => v1.X.CompareTo(v2.X));
                newps.AddRange(xlocs);
            }
            D1ChartDatas.Clear();
            UpdatePlotChart();
            //进行实验
            SetStartTime(DateTime.Now);
            var locs = (GetDeviceByName("LenZ") as NanoStageInfo).Device.Position;
            ParentPage.Chart2D.LockPlotCursor();
            foreach (var item in newps)
            {
                //刷新可用设备
                GetDevices();
                var lz = GetDeviceByName("LenZ") as NanoStageInfo;
                DeviceDispatcher.UseDevices(lz);
                lz.Device.MoveToAndWait(locs, 3000);
                DeviceDispatcher.EndUseDevices(lz);

                //移动到目标点
                MoveToCursor(item);
                int autotraceCount = GetInputParamValueByName("AutotraceNumber");
                for (int i = 0; i < autotraceCount; i++)
                {
                    var autotraceexp = RunSubExperimentBlock(0, true);
                    if (i == autotraceCount - 1)
                    {
                        //添加数据
                        var locxs = autotraceexp.Get1DChartData("位置", "AutoTrace X") as NumricChartData1D;
                        var countxs = autotraceexp.Get1DChartData("计数", "AutoTrace X") as NumricChartData1D;
                        var locys = autotraceexp.Get1DChartData("位置", "AutoTrace Y") as NumricChartData1D;
                        var countys = autotraceexp.Get1DChartData("计数", "AutoTrace Y") as NumricChartData1D;
                        var loczs = autotraceexp.Get1DChartData("位置", "AutoTrace Z") as NumricChartData1D;
                        var countzs = autotraceexp.Get1DChartData("计数", "AutoTrace Z") as NumricChartData1D;
                        locxs.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + locxs.Name;
                        locys.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + locys.Name;
                        loczs.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + loczs.Name;
                        countxs.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + countxs.Name;
                        countys.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + countys.Name;
                        countzs.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + countzs.Name;
                        D1ChartDatas.Add(locxs);
                        D1ChartDatas.Add(locys);
                        D1ChartDatas.Add(loczs);
                        D1ChartDatas.Add(countxs);
                        D1ChartDatas.Add(countys);
                        D1ChartDatas.Add(countzs);
                        UpdatePlotChart();
                    }
                }
                //测CW谱
                var cwexp = RunSubExperimentBlock(1, true);
                var freqs = cwexp.Get1DChartData("频率", "CW对比度数据") as NumricChartData1D;
                var contracts = cwexp.Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D;
                if (Get1DChartData("频率", "CW对比度数据") == null)
                    D1ChartDatas.Add(freqs);
                contracts.Name = " X: " + Math.Round(item.X, 5).ToString() + " Y: " + Math.Round(item.Y, 5).ToString() + contracts.Name;
                D1ChartDatas.Add(contracts);
                UpdatePlotChart();
            }
            SetEndTime(DateTime.Now);
        }
        #endregion

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }
    }
}

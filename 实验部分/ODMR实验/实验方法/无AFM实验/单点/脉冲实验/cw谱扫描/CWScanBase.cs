using CodeHelper;
using Controls.Charts;
using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描
{
    public abstract class CWBase : PulseExpBase
    {
        public override string ODMRExperimentGroupName { get; set; } = "点实验";

        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>();

        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>();

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool IsAFMSubExperiment { get; protected set; } = true;

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private List<object> FirstScanEvent(RFSourceInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            //新建数据集
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("频率","CW对比度数据",ChartDataType.X),
                new NumricChartData1D("对比度","CW对比度数据",ChartDataType.Y),
                new NumricChartData1D("频率","CW荧光计数",ChartDataType.X),
                new NumricChartData1D("信号总计数","CW荧光计数",ChartDataType.Y),
                new NumricChartData1D("参考信号总计数","CW荧光计数",ChartDataType.Y),
            };
            UpdatePlotChart();
            Show1DChartData("CW对比度数据", "频率", "对比度");
            return ScanEvent(device, range, locvalue, inputParams);
        }

        private List<object> ScanEvent(RFSourceInfo device, D1NumricScanRangeBase range, double locvalue, List<object> inputParams)
        {
            PulsePhotonPack pack = DoPulseExp("CW", locvalue, GetRFPower(), GetLoopCount(), 4, GetPointTimeout());

            double signal = pack.GetPhotonsAtIndex(0).Sum();
            double reference = pack.GetPhotonsAtIndex(1).Sum();
            double contrast = 0;
            try
            {
                contrast = (signal - reference) / reference;
            }
            catch (Exception)
            {
            }

            (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data.Add(contrast);
            (Get1DChartData("频率", "CW荧光计数") as NumricChartData1D).Data.Add(locvalue);
            (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(signal);
            (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data.Add(reference);
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public abstract List<double> GetScanFrequences();

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }

        public override void PreExpEventWithoutAFM()
        {
        }

        public override void AfterExpEventWithoutAFM()
        {
            SetOutput();
        }

        /// <summary>
        /// 获取循环次数
        /// </summary>
        /// <returns></returns>
        protected abstract int GetLoopCount();

        /// <summary>
        /// 获取单点超时时间
        /// </summary>
        /// <returns></returns>
        protected abstract int GetPointTimeout();

        /// <summary>
        /// 获取微波功率
        /// </summary>
        /// <returns></returns>
        protected abstract double GetRFPower();

        /// <summary>
        /// 扫描完成后设置输出结果
        /// </summary>
        protected abstract void SetOutput();

        public override void ODMRExpWithoutAFM()
        {
            Scan1DSession<RFSourceInfo> session = new Scan1DSession<RFSourceInfo>();
            session.FirstScanEvent = FirstScanEvent;
            session.ScanEvent = ScanEvent;
            var dev = GetDeviceByName("RFSource") as RFSourceInfo;
            session.ScanSource = dev;
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.ProgressBarMethod = new Action<RFSourceInfo, double>((sour, v) =>
            {
                SetProgress(v);
            });
            session.SetStateMethod = new Action<RFSourceInfo, double>((sour, v) =>
            {
                SetExpState("CW谱扫描,当前频率:" + Math.Round(v, 5).ToString());
            });
            session.BeginScan(new D1NumricListScanRange(GetScanFrequences()), 0, 100);
        }

        protected List<double> GetFrequences()
        {
            return (Get1DChartData("频率", "CW对比度数据") as NumricChartData1D).Data;
        }

        protected List<double> GetContracts()
        {
            return (Get1DChartData("对比度", "CW对比度数据") as NumricChartData1D).Data; ;
        }

        protected List<double> GetReferenceCounts()
        {
            return (Get1DChartData("参考信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }

        protected List<double> GetSignalCounts()
        {
            return (Get1DChartData("信号总计数", "CW荧光计数") as NumricChartData1D).Data; ;
        }
    }
}

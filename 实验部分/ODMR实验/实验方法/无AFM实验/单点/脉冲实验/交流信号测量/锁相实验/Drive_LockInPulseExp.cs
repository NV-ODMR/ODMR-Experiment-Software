using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ODMR_Lab.设备部分.其他设备;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.梯度测量相关实验
{
    internal class Drive_LockInPulseExp : ODMRExperimentWithoutAFM
    {

        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "变电压锁相相位实验";

        public override string Description { get; set; } = "";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<double>("锁相信号振幅起始值（V）",0,"SignalAmpStart"),
            new Param<double>("锁相信号振幅终止值（V）",5,"SignalAmpEnd"),
            new Param<int>("锁相信号扫描点数",5,"SignalAmpCount"),
            new Param<bool>("往返扫描",true,"IsReverse"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();
        public override bool IsAFMSubExperiment { get; protected set; } = false;
        public override string ODMRExperimentGroupName { get; set; } = "点实验";
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.源表,new Param<string>("电源","","LocInPower")),
        };

        public override void AfterExpEventWithoutAFM()
        {

        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        D1NumricLinearScanRange range0 = null;

        public override void ODMRExpWithoutAFM()
        {
            double lo = GetInputParamValueByName("SignalAmpStart");
            double hi = GetInputParamValueByName("SignalAmpEnd");
            int count = GetInputParamValueByName("SignalAmpCount");
            List<double> scan = Enumerable.Range(0, count).Select(x => lo + (hi - lo) * x / (count - 1)).ToList();
            if (GetInputParamValueByName("IsReverse"))
            {
                List<double> rev = scan.ToArray().ToList();
                rev.Reverse();
                rev.RemoveAt(0);
                scan.AddRange(rev);
            }
            int loop = 5;
            range0 = new D1NumricLinearScanRange(lo, hi, count, false);
            Get1DChartDataSource(D1ChartDatas.Last().Name, "电压扫描数据").AddRange(range0.ScanPoints);
            for (int i = 0; i < loop; i++)
            {
                var data = new NumricChartData1D("第" + i.ToString() + "轮" + "布居度相位", "电压扫描数据", ChartDataType.Y);
                data.Data = Enumerable.Repeat(double.NaN, count).ToList();
                D1ChartDatas.Add(data);
                D1NumricLinearScanRange range = new D1NumricLinearScanRange(lo, hi, count, false);
                Scan1DSession<object> session = new Scan1DSession<object>();
                session.SetStateMethod = new Action<object, double>((obj, val) =>
                {
                    SetExpState("第" + i.ToString() + "轮," + "当前驱动电压(V):" + val.ToString());
                });
                session.ScanSource = new object();
                session.ProgressBarMethod = new Action<object, double>((obj, val) =>
                {
                    SetProgress(val);
                });
                session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
                session.FirstScanEvent = ScanEvent;
                session.ScanEvent = ScanEvent;
                session.BeginScan(range, 0, 100 / loop);
                if (GetInputParamValueByName("IsReverse"))
                {
                    var datarev = new NumricChartData1D("第" + i.ToString() + "轮反向" + "布居度相位", "电压扫描数据", ChartDataType.Y);
                    datarev.Data = Enumerable.Repeat(double.NaN, count).ToList();
                    D1ChartDatas.Add(datarev);
                    range = new D1NumricLinearScanRange(lo, hi, count, true);
                    session = new Scan1DSession<object>();
                    session.SetStateMethod = new Action<object, double>((obj, val) =>
                    {
                        SetExpState("第" + i.ToString() + "轮反向," + "当前驱动电压(V):" + val.ToString());
                    });
                    session.ScanSource = new object();
                    session.ProgressBarMethod = new Action<object, double>((obj, val) =>
                    {
                        SetProgress(val);
                    });
                    session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
                    session.FirstScanEvent = ScanEvent;
                    session.ScanEvent = ScanEvent;
                    session.BeginScan(range, 0, 100 / loop);
                }
            }
        }

        private List<object> ScanEvent(object arg1, D1NumricScanRangeBase @base, double arg3, List<object> list)
        {
            //设置驱动电压
            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.VoltageRampStep = Math.Abs(@base.Hi - @base.Lo) / 50.0;
            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.VoltageRampGap = 100;

            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.TargetVoltage = arg3;

            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.Measure();

            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.TargetVoltage = arg3;

            (GetDeviceByName("LocInPower") as PowerMeterInfo).Device.Measure();

            var exp = RunSubExperimentBlock(0, true);

            if (exp.GetOutputParamValueByName("PPhase") == null)
            {
                throw new Exception("请将子实验中的参考电压选项关闭");
            }
            Get1DChartDataSource(D1ChartDatas.Last().Name, "电压扫描数据")[range0.GetNearestIndex(arg3)] = exp.GetOutputParamValueByName("PPhase");
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            JudgeThreadEndOrResumeAction?.Invoke();
            return new List<object>();
        }

        public override void PreExpEventWithoutAFM()
        {
            D1ChartDatas = new List<ChartData1D>()
            {
                new NumricChartData1D("电压值", "电压扫描数据", ChartDataType.X)
            };
        }


        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
               new LockInPulseExp()
            };
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }
    }
}

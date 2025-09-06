using Controls.Charts;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.梯度测量相关实验
{
    public class Distance_Fluorescence_Measure : VibrationExpBase
    {
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "距离-荧光曲线测量";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<bool>("自动Trace",false,"UseAutoTrace"),
            new Param<int>("Trace间隔",10,"TraceGap"),
            new Param<bool>("尝试多次下针",true,"MultiAFMDrop"),
            new Param<int>("最大尝试下针次数",5,"DropCount"),
            new Param<double>("尝试下针失败后样品移动量",0.005,"DropDet"),
            new Param<bool>("样品轴升高方向反向",false,"SampleAxisReverse"),
            new Param<double>("最大限制电压(V)",10,"UpperLimit"),
            new Param<double>("测量时积分系数I",-3,"Measure_I"),
            new Param<double>("距离起始点(nm)",0,"StartDistance"),
            new Param<double>("距离终止点(nm)",100,"EndDistance"),
            new Param<int>("测量点数",20,"PointCount"),
            new Param<double>("电压/位移系数(V/μm)",1.14,"Voltage_Displacement_Ratio"),
            new Param<int>("序列循环次数",10000,"SeqLoopCount"),
            new Param<int>("超时时间(ms)",1000,"TimeOut"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.锁相放大器,new Param<string>("Lock In","","LockIn")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.样品位移台,new Param<string>("样品Z轴","","SampleZ")),
        };
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>() { };
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>() { };


        public override void ODMRExpWithoutAFM()
        {
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("StartDistance"), GetInputParamValueByName("EndDistance"), GetInputParamValueByName("PointCount"));
            Scan1DSession<object> session = new Scan1DSession<object>();
            session.SetStateMethod = new Action<object, double>((obj, val) =>
            {
                SetExpState("当前样品-探针距离(V):" + val.ToString());
            });
            session.ScanSource = new object();
            session.ProgressBarMethod = new Action<object, double>((obj, val) =>
            {
                SetProgress(val);
            });
            session.StateJudgeEvent = JudgeThreadEndOrResumeAction;
            session.FirstScanEvent = ScanEvent;
            session.ScanEvent = ScanEvent;
            session.BeginScan(range, 0, 100);
        }

        public override void PreExpEventWithoutAFM()
        {
            MultiDropProcedure(GetInputParamValueByName("MultiAFMDrop"), GetInputParamValueByName("SampleAxisReverse"), GetInputParamValueByName("DropCount"), GetInputParamValueByName("DropDet"), GetDeviceByName("SampleZ") as NanoStageInfo, GetDeviceByName("LockIn") as LockinInfo);
            D1ChartDatas = new List<ChartData1D>()
            {
               new NumricChartData1D("距离(nm)","样品-探针距离测试",ChartDataType.X),
               new NumricChartData1D("距离(V)","样品-探针距离测试",ChartDataType.X),
               new NumricChartData1D("荧光光子数","样品-探针距离测试",ChartDataType.Y)
            };
        }

        public override void AfterExpEventWithoutAFM()
        {
            //撤针
            SetExpState("正在撤针...");
            AFMStopDrop drop = new AFMStopDrop();
            drop.CoreMethod(new List<object>(), GetDeviceByName("LockIn"));
            SetExpState("");
            //用线性拟合计算荧光-距离关系
            var xs = Get1DChartDataSource("距离(nm)", "样品-探针距离测试");
            var ys_x = Get1DChartDataSource("荧光光子数", "样品-探针距离测试");
            double a = 0;
            double b = 0;
            double[] ps_x = CurveFitting.FitCurveWithFunc(xs, ys_x, new List<double>() { a, b }, new List<double>() { 10, 10 }, LinearFunc, AlgorithmType.LevenbergMarquardt, 20000);
            var ftxs = new D1NumricLinearScanRange(xs.Min(), xs.Max(), 500).ScanPoints;
            var fitys_x = ftxs.Select(x => LinearFunc(x, ps_x)).ToList();
            D1FitDatas.Add(new FittedData1D("a*x+b", "x", new List<string>() { "a", "b" }, ps_x.ToList(), "距离(nm)", "样品-探针距离测试", new NumricDataSeries("拟合曲线", ftxs, fitys_x) { LineColor = Colors.LightSkyBlue }));
            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            Show1DFittedData("拟合曲线");
            OutputParams.Add(new Param<double>("曲线斜率", ps_x[0], "Slope"));
            OutputParams.Add(new Param<double>("曲线截距", ps_x[1], "Bias"));
        }

        private double LinearFunc(double x, double[] ps)
        {
            return x * ps[0] + ps[1];
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>();
        }


        private List<object> ScanEvent(object arg1, D1NumricScanRangeBase range, double arg3, List<object> list)
        {
            //下针到指定距离
            AFMFloatDrop floadrop = new AFMFloatDrop();
            var result = floadrop.CoreMethod(new List<object>() { GetInputParamValueByName("UpperLimit"), arg3 * GetInputParamValueByName("Voltage_Displacement_Ratio") / 1000, GetInputParamValueByName("Measure_I") }, GetDeviceByName("LockIn"));
            if (((bool)result[0]) == false) throw new Exception();
            //测量光子计数
            PulsePhotonPack photonpack = DoPulseExp("DelayCountTest", 2870, -20, GetInputParamValueByName("SeqLoopCount"), 2, GetInputParamValueByName("TimeOut"));
            double photoncount = photonpack.GetPhotonsAtIndex(0).Sum();
            //获取当前高度
            var disvolt = Get1DChartDataSource("距离(V)", "样品-探针距离测试");
            var disnm = Get1DChartDataSource("距离(nm)", "样品-探针距离测试");
            var count = Get1DChartDataSource("荧光光子数", "样品-探针距离测试");
            double heightvolt = (GetDeviceByName("LockIn") as LockinInfo).Device.PIDValue;
            disvolt.Add(heightvolt);
            disnm.Add(arg3);
            count.Add(photoncount);
            return new List<object>();
        }

        public override bool PreConfirmProcedure()
        {
            if (GetInputParamValueByName("StartDistance") < 0 || GetInputParamValueByName("EndDistance") < 0)
            {
                throw new Exception("样品-探针距离不能为负值");
            }

            if (!DropConfirm(GetDeviceByName("LockIn") as LockinInfo)) return false;
            return true;
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }
    }
}

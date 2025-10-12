using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他.磁场调节.通用方法;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ODMR_Lab.设备部分.相机_翻转镜;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.其他;
using System.Threading;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.梯度测量相关实验
{
    internal class Drive_Vibration_Relation : VibrationExpBase
    {
        public override List<KeyValuePair<DeviceTypes, Param<string>>> PulseExpDevices { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.锁相放大器,new Param<string>("Lock In","","LockIn")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.样品位移台,new Param<string>("样品Z轴","","SampleZ")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.信号发生器通道,new Param<string>("锁相信号通道","","LockInSignalChannel")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.开关,new Param<string>("锁相信号开关","","LockInSignalSwitch")),
        };
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override string ODMRExperimentName { get; set; } = "变驱动电压测量样品振幅";

        public override string Description { get; set; } = "";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<string>("距离-荧光曲线文件名","","FluorescenceFileName"),
            new Param<bool>("自动Trace",false,"UseAutoTrace"),
            new Param<int>("Trace间隔",10,"TraceGap"),
            new Param<bool>("尝试多次下针",true,"MultiAFMDrop"),
            new Param<int>("最大尝试下针次数",5,"DropCount"),
            new Param<double>("尝试下针失败后样品移动量",0.005,"DropDet"),
            new Param<bool>("样品轴升高方向反向",false,"SampleAxisReverse"),
            new Param<double>("测量距离(nm)",20,"MeasureDistance"),
            new Param<double>("电压/位移系数(V/μm)",1.14,"Voltage_Displacement_Ratio"),
            new Param<double>("最大限制电压(V)",10,"UpperLimit"),
            new Param<double>("测量时积分系数I",-3,"Measure_I"),

            new Param<double>("驱动电压起始点(V)",0,"VoltStart"),
            new Param<double>("驱动电压终止点(V)",5,"VoltEnd"),
            new Param<int>("驱动电压扫描点数",5,"VoltScanPoint"),

            new Param<bool>("子实验每轮扫描结束后重新下针",true,"RedropAfterLoop"),
        };

        private string FluorescenceFilePath = "";
        private double FluorescenceSlope = double.NaN;
        private double FluorescenceBias = double.NaN;


        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();
        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override void AfterExpEventWithoutAFM()
        {
            //撤针
            SetExpState("正在撤针...");
            AFMStopDrop drop = new AFMStopDrop();
            drop.CoreMethod(new List<object>(), GetDeviceByName("LockIn"));
            SetExpState("");
            (GetDeviceByName("LockIn") as LockinInfo).Device.PIDOutputUpperLimit = GetInputParamValueByName("UpperLimit");
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }

        public override void ODMRExpWithoutAFM()
        {
            if (GetInputParamValueByName("RedropAfterLoop"))
            {
                (SubExperiments[0] as LockInDelayCountTest).LoopEndMethod = new Action(() =>
                 {
                     SwitchInfo sw = GetDeviceByName("LockInSignalSwitch") as SwitchInfo;
                     //sw.Device.IsOpen = false;
                     //重新下针
                     AFMFloatDrop floatdrop = new AFMFloatDrop();
                     var result = floatdrop.CoreMethod(new List<object>() { GetInputParamValueByName("UpperLimit"),
                GetInputParamValueByName("MeasureDistance") * GetInputParamValueByName("Voltage_Displacement_Ratio") / 1000, GetInputParamValueByName("Measure_I") }, GetDeviceByName("LockIn"));
                     if ((bool)result[0] == false) throw new Exception();
                     sw.Device.IsOpen = true;
                     RunSubExperimentBlock(1);
                     //Thread.Sleep(20000);
                 });
            }
            else
            {
                (SubExperiments[0] as LockInDelayCountTest).LoopEndMethod = null;
            }
            D1NumricLinearScanRange range = new D1NumricLinearScanRange(GetInputParamValueByName("VoltStart"), GetInputParamValueByName("VoltEnd"), GetInputParamValueByName("VoltScanPoint"));
            Scan1DSession<object> session = new Scan1DSession<object>();
            session.SetStateMethod = new Action<object, double>((obj, val) =>
            {
                SetExpState("当前驱动电压(V):" + val.ToString());
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

        private List<object> ScanEvent(object arg1, D1NumricScanRangeBase range, double arg3, List<object> list)
        {
            //关闭信号
            //(GetDeviceByName("LockInSignalSwitch") as SwitchInfo).Device.IsOpen = false;
            //设置驱动电压
            (GetDeviceByName("LockInSignalChannel") as SignalGeneratorChannelInfo).Device.Amplitude = arg3;

            //AutoTrace
            RunSubExperimentBlock(1, true);

            //重新下针
            AFMFloatDrop floatdrop = new AFMFloatDrop();
            var result = floatdrop.CoreMethod(new List<object>() { GetInputParamValueByName("UpperLimit"),
                GetInputParamValueByName("MeasureDistance") * GetInputParamValueByName("Voltage_Displacement_Ratio") / 1000, GetInputParamValueByName("Measure_I") }, GetDeviceByName("LockIn"));
            if ((bool)result[0] == false) throw new Exception();

            //打开信号
            (GetDeviceByName("LockInSignalSwitch") as SwitchInfo).Device.IsOpen = true;
            Thread.Sleep(3000);

            //测量荧光振动曲线
            var exp = RunSubExperimentBlock(0, true);
            var photons = exp.Get1DChartDataSource("光子数", "Delay测试数据").ToArray().ToList();
            var times = exp.Get1DChartDataSource("时间(ns)", "Delay测试数据").ToArray().ToList();
            double amplitude = exp.GetOutputParamValueByName("Amplitude");
            double average = exp.GetOutputParamValueByName("Average");

            D1ChartDatas.Add(new NumricChartData1D("时间(ns)--" + "驱动振幅" + arg3.ToString() + "V", "振动光子数数据", ChartDataType.X) { Data = times });
            D1ChartDatas.Add(new NumricChartData1D("光子数--" + "驱动振幅" + arg3.ToString() + "V", "振动光子数数据", ChartDataType.Y) { Data = photons });

            var volts = Get1DChartDataSource("驱动电压幅度（V）", "样品振幅测试");
            var amps = Get1DChartDataSource("光子数振幅", "样品振幅测试");
            var aves = Get1DChartDataSource("光子数平均值", "样品振幅测试");
            var disamps = Get1DChartDataSource("距离测量值(nm)", "样品振幅测试");
            var disaves = Get1DChartDataSource("振幅计算值(nm)", "样品振幅测试");

            //根据测到的荧光距离曲线计算实际距离
            double dist = (average - FluorescenceBias) / FluorescenceSlope;
            double disamp = Math.Abs(amplitude / FluorescenceSlope);

            volts.Add(arg3);
            amps.Add(amplitude);
            aves.Add(average);
            disamps.Add(dist);
            disaves.Add(disamp);

            UpdatePlotChart();
            UpdatePlotChartFlow(true);
            return new List<object>();
        }

        public override bool PreConfirmProcedure()
        {
            if (GetInputParamValueByName("StartDistance") < 0 || GetInputParamValueByName("EndDistance") < 0)
            {
                throw new Exception("样品-探针距离不能为负值");
            }

            if (FluorescenceFilePath == "" || double.IsNaN(FluorescenceSlope) || double.IsNaN(FluorescenceBias))
            {
                throw new Exception("请先导入曲线文件");
            }

            GetDevices();
            //下针信息确认
            if (!DropConfirm(GetDeviceByName("LockIn") as LockinInfo)) return false;
            if (MessageWindow.ShowMessageBox("提示", "此操作将改变锁相信号强度,这可能会使音叉共振幅度发生改变,同时会清除原先的实验数据,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override void PreExpEventWithoutAFM()
        {
            //下针
            MultiDropProcedure(GetInputParamValueByName("MultiAFMDrop"), GetInputParamValueByName("SampleAxisReverse"), GetInputParamValueByName("DropCount"), GetInputParamValueByName("DropDet"), GetDeviceByName("SampleZ") as NanoStageInfo, GetDeviceByName("LockIn") as LockinInfo);
            D1ChartDatas = new List<ChartData1D>()
            {
               new NumricChartData1D("驱动电压幅度（V）","样品振幅测试",ChartDataType.X),
               new NumricChartData1D("光子数振幅","样品振幅测试",ChartDataType.Y),
               new NumricChartData1D("光子数平均值","样品振幅测试",ChartDataType.Y),
               new NumricChartData1D("距离测量值(nm)","样品振幅测试",ChartDataType.Y),
               new NumricChartData1D("振幅计算值(nm)","样品振幅测试",ChartDataType.Y)
            };
        }

        protected override List<KeyValuePair<string, Action>> AddPulseInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>() {
             new KeyValuePair<string, Action>("导入距离-荧光曲线",ImportCurveFile)
            };
        }

        private void ImportCurveFile()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SequenceFileExpObject fobj = new SequenceFileExpObject();
                if (fobj.ReadFromExplorer(out string path))
                {
                    if (fobj.ODMRExperimentName != "距离-荧光曲线测量" || fobj.ODMRExperimentGroupName != "梯度测量相关实验")
                    {
                        MessageWindow.ShowTipWindow("导入文件类型不符", Window.GetWindow(ParentPage));
                    }
                    FluorescenceFilePath = path;
                    SetInputParamValueByName("FluorescenceFileName", Path.GetFileName(path));
                    try
                    {
                        FluorescenceSlope = double.Parse(fobj.GetOutputParamValueByName("Slope"));
                        FluorescenceBias = double.Parse(fobj.GetOutputParamValueByName("Bias"));
                        MessageWindow.ShowTipWindow("文件导入成功\n" + "曲线斜率:" + FluorescenceSlope + "\n" + "曲线截距:" + FluorescenceBias, Window.GetWindow(ParentPage));
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageWindow.ShowTipWindow("导入文件失败\n" + ex.Message, Window.GetWindow(ParentPage));
                    }
                }
                MessageWindow.ShowTipWindow("导入文件失败，文件格式不支持", Window.GetWindow(ParentPage));
            });
        }

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>()
            {
                new LockInDelayCountTest(),
                new AutoTrace()
            };
        }
    }
}

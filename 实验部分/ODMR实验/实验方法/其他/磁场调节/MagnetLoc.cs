using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Controls.Charts;
using Controls.Windows;
using HardWares.射频源.Rigol_DSG_3060;
using MathLib.NormalMath.Decimal;
using MathLib.NormalMath.Decimal.Function;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.Python管理器;
using ODMR_Lab.基本控件;
using ODMR_Lab.基本控件.一维图表;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.CW谱扫描;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.光子探测器;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.设备部分.板卡;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    /// <summary>
    /// AutoTrace定位NV
    /// </summary>
    public partial class MagnetLoc : ODMRExperimentWithoutAFM
    {
        public override string ODMRExperimentName { get; set; } = "磁场定位（确定NV朝向）";

        public override string ODMRExperimentGroupName { get; set; } = "定位操作";
        public override bool IsAFMSubExperiment { get; protected set; } = false;
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
          new Param<double>("磁铁半径(mm)", double.NaN, "MRadius"),
          new Param<double>("磁铁长度(mm)", double.NaN, "MLength"),
          new Param<double>("磁铁磁化强度系数", double.NaN, "MIntensity"){ Helper="磁化强度系数,可以通过实验附带的对应计算得到"},
          new Param<double>("偏心量X(mm)", double.NaN, "OffsetX"){ Helper="偏心系数,可以通过实验附带的对应计算得到"},
          new Param<double>("偏心量Y(mm)", double.NaN, "OffsetY"){ Helper="偏心系数,可以通过实验附带的对应计算得到"},
          new Param<bool>("反转X轴", false, "ReverseX"){ Helper="如果X方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
          new Param<bool>("反转Y轴", false, "ReverseY"){ Helper="如果Y方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
          new Param<bool>("反转Z轴", false, "ReverseZ"){ Helper="如果Z方向位移台示数增加导致磁铁和NV的距离减小则要勾选此选项"},
          new Param<bool>("反转角度", false, "ReverseA"){ Helper="如果角度方向位移台的示数增加方向和规定的X轴的正向不同，则要勾选此选项"},
          new Param<double>("角度零点(度)", double.NaN, "AngleStart"){ Helper="当圆柱形磁铁轴向沿X轴时角度位移台的读数"},
          new Param<double>("X扫描范围上限(mm)", double.NaN, "XScanHi"),
          new Param<double>("X扫描范围下限(mm)", double.NaN, "XScanLo"),
          new Param<double>("Y扫描范围上限(mm)", double.NaN, "YScanHi"),
          new Param<double>("Y扫描范围下限(mm)", double.NaN, "YScanLo"),
          new Param<double>("Z扫描高度(mm)", double.NaN, "ZPlane"){ Helper="定位过程中磁铁保持的高度"},
          new Param<double>("D值(Mhz)", double.NaN, "D")
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {

        };
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁X轴","","MagnetX")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Y轴","","MagnetY")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁Z轴","","MagnetZ")),
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.磁铁位移台,new Param<string>("磁铁角度轴","","MagnetAngle")),
        };
        public override List<ODMRExpObject> SubExperiments { get; set; } = new List<ODMRExpObject>()
        {
            new AutoTrace(),
            new TotalCW()
        };

        public override List<ChartData1D> D1ChartDatas { get; set; } = new List<ChartData1D>();
        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();
        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        public override void AfterExpEventWithoutAFM()
        {
        }

        public override List<ParentPlotDataPack> GetD1PlotPacks()
        {
            return new List<ParentPlotDataPack>();
        }


        public override void ODMRExpWithoutAFM()
        {
            SetExpState("正在扫描X轴...");
            //移动Z轴
            //(GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(GetInputParamValueByName("ZPlane"), 10000);
            JudgeThreadEndOrResumeAction?.Invoke();
            //旋转台移动到轴向沿Y
            //(GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetAngleX(), 60000);
            //X方向扫描
            ScanX(0, 20);
            SetExpState("正在扫描Y轴...");
            //旋转台移动到轴向沿X
            //(GetDeviceByName("MagnetAngle") as NanoStageInfo).Device.MoveToAndWait(GetAngleY(), 60000);
            JudgeThreadEndOrResumeAction?.Invoke();
            //移动X到最大值
            //(GetDeviceByName("MagnetX") as NanoStageInfo).Device.MoveToAndWait(GetOutputParamValueByName("XLoc"), 60000);
            //Y方向扫描
            ScanY(20, 40);
            JudgeThreadEndOrResumeAction?.Invoke();
            SetExpState("正在扫描Z轴...");
            //移动Y到最大值
            //(GetDeviceByName("MagnetY") as NanoStageInfo).Device.MoveToAndWait(GetOutputParamValueByName("YLoc"), 10000);
            JudgeThreadEndOrResumeAction?.Invoke();
            //Z扫描
            //ScanZ(40, 60);
            var bps = new List<double>() { 17.86237825, 13.23614853 };
            var bs = new List<double>() { 71.47079267, 55.36433278 };
            var locs = new List<double>() { 21.5, 20.5 };

            double ratio = bps[0] / bps[1];
            double zloc = 0;
            if (ratio < 1)
            {
                ratio = bps[1] / bps[0];
                zloc = locs[1];
            }
            else
            {
                zloc = locs[0];
            }
            if (double.IsInfinity(ratio))
            {
                ratio = bs[0] / bs[1];
                zloc = locs[0];
                if (ratio < 1)
                {
                    ratio = bs[1] / bs[0];
                    zloc = locs[1];
                }
            }
            OutputParams.Add(new Param<double>("Z方向参考位置", zloc, "ZLoc"));
            OutputParams.Add(new Param<double>("参考位置与NV的距离", new Magnet(GetInputParamValueByName("MRadius"), GetInputParamValueByName("MLength"), 1).FindZRoot(locs[0], locs[1], ratio), "ZDistance"));

            JudgeThreadEndOrResumeAction?.Invoke();
            //角度扫描
            SetExpState("正在扫描角度轴...");
            (GetDeviceByName("MagnetZ") as NanoStageInfo).Device.MoveToAndWait(GetOutputParamValueByName("ZLoc"), 10000);
            JudgeThreadEndOrResumeAction?.Invoke();
            //ScanAngle(60, 80);
            double t1 = 57.93253;
            OutputParams.Add(new Param<double>("θ值1", t1, "Theta1"));
            OutputParams.Add(new Param<double>("θ值2", 180 - t1, "Theta2"));
            double phase = 191.8347;
            double phi2 = phase + 180;
            while (phi2 > 360)
            {
                phi2 -= 360;
            }
            while (phi2 < -360)
            {
                phi2 += 360;
            }
            OutputParams.Add(new Param<double>("φ值1", phase, "Phi1"));
            OutputParams.Add(new Param<double>("φ值2", phi2, "Phi2"));
            JudgeThreadEndOrResumeAction?.Invoke();
            //角度检查
            CheckAngle(80, 100);
        }

        public override bool PreConfirmProcedure()
        {
            if (MessageWindow.ShowMessageBox("提示", "历史数据将被清除,是否要继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        public override void PreExpEventWithoutAFM()
        {
            //新建图表
            D1ChartDatas = new List<ChartData1D>()
            {
                #region X方向信息
                new NumricChartData1D("位置","X方向磁场信息",ChartDataType.X),
                new NumricChartData1D("CW谱位置1","X方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("CW谱位置2","X方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("沿轴磁场(G)","X方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("垂直轴磁场(G)","X方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("总磁场(G)","X方向磁场信息",ChartDataType.Y),
                #endregion

                #region Y方向信息
                new NumricChartData1D("位置","Y方向磁场信息",ChartDataType.X),
                new NumricChartData1D("CW谱位置1","Y方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("CW谱位置2","Y方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("沿轴磁场(G)","Y方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("垂直轴磁场(G)","Y方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("总磁场(G)","Y方向磁场信息",ChartDataType.Y),
                #endregion

                #region 角度方向信息
                new NumricChartData1D("位置","角度方向磁场信息",ChartDataType.X),
                new NumricChartData1D("CW谱位置1","角度方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("CW谱位置2","角度方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("沿轴磁场(G)","角度方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("垂直轴磁场(G)","角度方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("总磁场(G)","角度方向磁场信息",ChartDataType.Y),
                #endregion

                #region Z方向信息
                new NumricChartData1D("位置","Z方向磁场信息",ChartDataType.X),
                new NumricChartData1D("CW谱位置1","Z方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("CW谱位置2","Z方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("沿轴磁场(G)","Z方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("垂直轴磁场(G)","Z方向磁场信息",ChartDataType.Y),
                new NumricChartData1D("总磁场(G)","Z方向磁场信息",ChartDataType.Y),
                #endregion

                #region 角度检查信息
                new NumricChartData1D("位置","角度检查信息",ChartDataType.X),
                new NumricChartData1D("CW谱位置1","角度检查信息",ChartDataType.Y),
                new NumricChartData1D("CW谱位置2","角度检查信息",ChartDataType.Y),
                new NumricChartData1D("沿轴磁场(G)","角度检查信息",ChartDataType.Y),
                new NumricChartData1D("垂直轴磁场(G)","角度检查信息",ChartDataType.Y),
                new NumricChartData1D("总磁场(G)","角度检查信息",ChartDataType.Y),
                #endregion
            };

            UpdatePlotChart();
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            var btns = new List<KeyValuePair<string, Action>>();
            btns.Add(new KeyValuePair<string, Action>("偏心参数计算", ShowOffsetWindow));
            btns.Add(new KeyValuePair<string, Action>("磁场预测", ShowPredictWindow));
            return btns;
        }

        #region 交互部分
        OffsetWindow offsetwin = null;
        private void ShowOffsetWindow()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (offsetwin == null)
                    offsetwin = new OffsetWindow(this);
                offsetwin.Owner = Window.GetWindow(ParentPage);
                offsetwin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                offsetwin.Show();
            });
        }

        PredictWindow predictwin = null;
        private void ShowPredictWindow()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (predictwin == null)
                    predictwin = new PredictWindow(this);
                predictwin.Owner = Window.GetWindow(ParentPage);
                predictwin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                predictwin.Show();
            });
        }
        #endregion
    }
}

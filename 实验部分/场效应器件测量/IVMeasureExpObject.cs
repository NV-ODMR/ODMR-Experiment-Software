using CodeHelper;
using Controls.Windows;
using HardWares.源表;
using ODMR_Lab.场效应器件测量;
using ODMR_Lab.基本控件;
using ODMR_Lab.数据处理;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分.源表;

namespace ODMR_Lab.实验部分.场效应器件测量
{
    public class IVMeasureExpObject : ExperimentObject<IVMeasureExpParams, IVMeasureConfigParams>
    {
        #region 数据及IO部分
        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.源表IV测量数据;

        /// <summary>
        /// 电流测量值
        /// </summary>
        public List<double> IVIData { get; set; } = new List<double>();

        /// <summary>
        /// 电压测量值
        /// </summary>
        public List<double> IVVData { get; set; } = new List<double>();

        /// <summary>
        /// 电压设置值
        /// </summary>
        public List<double> IVTatgetData { get; set; } = new List<double>();

        /// <summary>
        /// 测量时间
        /// </summary>
        public List<DateTime> IVTimes { get; set; } = new List<DateTime>();

        public override IVMeasureExpParams Param { get; set; } = new IVMeasureExpParams();

        public override IVMeasureConfigParams Config { get; set; } = new IVMeasureConfigParams();

        protected override void InnerRead(FileObject fobj)
        {
            IVIData = fobj.ExtractDouble("I_Measure");
            IVVData = fobj.ExtractDouble("V_Measure");
            IVTatgetData = fobj.ExtractDouble("V_Target");
            IVTimes = fobj.ExtractDate("Times");

            //添加扫描路径点
            if (fobj.Descriptions.Keys.Contains("ScanPoints"))
            {
                List<string> strs = fobj.Descriptions["ScanPoints"].Split(',').ToList();
                Config.ScanPoints.Clear();
                foreach (var item in strs)
                {
                    try
                    {
                        Config.ScanPoints.Add(double.Parse(item.Replace("V", "")));
                    }
                    catch (Exception) { }
                }
            }
        }

        protected override void InnerWrite(FileObject obj)
        {
            var res = Param.GenerateDescription();
            foreach (var item in res)
            {
                obj.Descriptions.Add(item.Key, item.Value);
            }
            //添加扫描路径点
            string points = "";
            foreach (var item in Config.ScanPoints)
            {
                points += item.ToString() + "V " + ",";
            }
            if (points != "") points = points.Remove(points.Length - 1, 1);

            obj.Descriptions.Add("ScanPoints", points);

            obj.WriteDoubleData("I_Measure", IVIData);
            obj.WriteDoubleData("V_Measure", IVVData);
            obj.WriteDoubleData("V_Target", IVTatgetData);
            obj.WriteDateData("Times", IVTimes);
        }

        protected override void InnerToDataVisualSource(DataVisualSource source)
        {
            source.Params.Add("实验类型", Enum.GetName(ExpType.GetType(), ExpType));
            Dictionary<string, string> temp = Param.GetPureDescription();
            foreach (var item in temp)
            {
                source.Params.Add(item.Key, item.Value);
            }

            string scanroute = "";
            foreach (var item in Config.ScanPoints)
            {
                scanroute += item.ToString() + "V ,";
            }
            if (scanroute != "")
            {
                scanroute = scanroute.Remove(scanroute.Length - 1, 1);
            }

            source.Params.Add("扫描路径", scanroute);

            source.ChartDataSource1D.Clear();
            source.ChartDataSource1D.Add(new TimeChartData1D("测量时间", "IV测量数据") { Data = IVTimes });
            source.ChartDataSource1D.Add(new NumricChartData1D("电流测量值(A)", "IV测量数据") { Data = IVIData });
            source.ChartDataSource1D.Add(new NumricChartData1D("电压测量值(V)", "IV测量数据") { Data = IVVData });
            source.ChartDataSource1D.Add(new NumricChartData1D("电压设定值(V)", "IV测量数据") { Data = IVTatgetData });
        }
        #endregion

        #region 实验线程部分
        public DisplayPage ExpPage { get; set; } = null;
        public override void ExperimentEvent()
        {

            ExpPage.IVResultWindow.SetTitle("IV测量结果，开始时间：" + DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss"));

            IVVData.Clear();
            IVTimes.Clear();
            IVIData.Clear();
            IVTatgetData.Clear();
            //测量

            Dev.Device.VoltageRampGap = Config.IVRampGap.Value;
            Dev.Device.VoltageRampStep = Config.IVRampStep.Value;
            Dev.Device.CurrentLimit = Config.IVCurrentLimit.Value;

            //停止自动采样
            Dev.AllowAutoMeasure = false;
            while (Dev.IsMeasuring)
            {
                Thread.Sleep(10);
            }
            SetProgress(0);


            double begin = 0;
            double end = 0;

            //预计总点数
            int count = 0;
            for (int i = 0; i < Config.ScanPoints.Count - 1; i++)
            {
                begin = Config.ScanPoints[i];
                end = Config.ScanPoints[i + 1];
                int sgn = (end - begin) > 0 ? 1 : -1;

                double temp = begin + Config.IVScanStep.Value * sgn;
                while ((temp - end) * sgn < 0)
                {
                    count += 1;
                    temp += Config.IVScanStep.Value * sgn;
                }
            }

            int ind = 0;
            MeasureGroup g;
            for (int i = 0; i < Config.ScanPoints.Count - 1; i++)
            {
                begin = Config.ScanPoints[i];
                end = Config.ScanPoints[i + 1];
                int sgn = (end - begin) > 0 ? 1 : -1;
                double temp = begin;
                while ((temp - end) * sgn < 0)
                {
                    JudgeThreadEndOrResumeAction?.Invoke();
                    Dev.Device.TargetVoltage = temp;
                    if (Math.Abs(temp) > 0.1)
                        Thread.Sleep(500);
                    Dev.Device.Output = true;
                    g = Dev.Device.Measure();
                    IVTimes.Add(g.TimeStamp);
                    IVIData.Add(g.Current);
                    IVTatgetData.Add(temp);
                    IVVData.Add(g.Voltage);
                    temp += Config.IVScanStep.Value * sgn;
                    //设置进度条
                    SetProgress(ind * 90.0 / count);
                    //更新结果
                    ExpPage.UpdateResult();
                    ind += 1;
                }
            }

            Dev.Device.TargetVoltage = end;
            g = Dev.Device.Measure();
            IVTimes.Add(g.TimeStamp);
            IVIData.Add(g.Current);
            IVTatgetData.Add(end);
            IVVData.Add(g.Voltage);

            //测量完成后返回0V
            Dev.Device.TargetVoltage = 0;
            //停止自动采样
            Dev.AllowAutoMeasure = true;
            SetProgress(100);
        }

        public override IVMeasureConfigParams ReadConfig()
        {
            IVMeasureConfigParams P = new IVMeasureConfigParams();
            P.ReadFromPage(new System.Windows.FrameworkElement[] { ExpPage });
            List<double> scanpoints = new List<double>() { 0 };
            string scams = "";
            foreach (var item in ExpPage.ScanPointPanel.Children)
            {
                try
                {
                    double value = double.Parse((item as TextBox).Text);
                    scanpoints.Add(value);
                    scams += value.ToString() + "V\n";
                }
                catch (Exception)
                {
                    continue;
                }
            }

            P.ScanPoints = scanpoints;
            return P;
        }

        PowerMeterInfo Dev = null;
        public override List<InfoBase> GetDevices()
        {
            if (ExpPage.IVDevice.SelectedItem == null)
            {
                throw new Exception("未选择进行IV测量的源表");
            }
            Dev = ExpPage.IVDevice.SelectedItem.Tag as PowerMeterInfo;
            return new List<InfoBase>() { Dev };
        }

        public override bool PreConfirmProcedure()
        {
            string scams = "";
            foreach (var item in ExpPage.ScanPointPanel.Children)
            {
                try
                {
                    double value = double.Parse((item as TextBox).Text);
                    scams += value.ToString() + "V\n";
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (MessageWindow.ShowMessageBox("提示", "扫描路径点:\n" + scams + "确定要继续吗?此操作将设置源表电压为非零值", MessageBoxButton.YesNo, owner: Window.GetWindow(ExpPage)) == MessageBoxResult.Yes)
            {
                return true;
            }

            return false;
        }
        #endregion
    }
}

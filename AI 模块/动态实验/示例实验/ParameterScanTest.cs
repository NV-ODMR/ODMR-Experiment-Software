using System;
using System.Collections.Generic;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;

namespace AI动态测试
{
    public class ParameterScanTest : ODMRExpObject
    {
        public override string ODMRExperimentName => "参数扫描测试";
        public override string ODMRExperimentGroupName => "AI动态测试";

        private List<ExpParamBase> _inputParams = new List<ExpParamBase>();
        public override List<ExpParamBase> InputParams
        {
            get { return _inputParams; }
            set { _inputParams = value; }
        }

        private List<ExpParamBase> _outputParams = new List<ExpParamBase>();
        public override List<ExpParamBase> OutputParams
        {
            get { return _outputParams; }
            set { _outputParams = value; }
        }

        private List<DeviceInfo> _deviceList = new List<DeviceInfo>();
        public override List<DeviceInfo> DeviceList
        {
            get { return _deviceList; }
        }

        private List<D1ChartData> _d1ChartDatas = new List<D1ChartData>();
        public override List<D1ChartData> D1ChartDatas
        {
            get { return _d1ChartDatas; }
        }

        private List<D1FitData> _d1FitDatas = new List<D1FitData>();
        public override List<D1FitData> D1FitDatas
        {
            get { return _d1FitDatas; }
        }

        private List<D2ChartData> _d2ChartDatas = new List<D2ChartData>();
        public override List<D2ChartData> D2ChartDatas
        {
            get { return _d2ChartDatas; }
        }

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        public override List<InfoBase> GetDevices()
        {
            return new List<InfoBase>();
        }

        public override void PreExpEvent()
        {
        }

        protected override void ExperimentCore()
        {
            SetExpState("参数扫描测试开始");

            // 读取参数
            int scanPoints = 20;
            double startFreq = 0;
            double endFreq = 10;
            foreach (var p in InputParams)
            {
                if (p.Name == "ScanPoints") scanPoints = (int)p.Value;
                else if (p.Name == "StartFreq") startFreq = (double)p.Value;
                else if (p.Name == "EndFreq") endFreq = (double)p.Value;
            }

            // 正确创建图表：X 和 Y 分开，使用 NumricChartData1D
            D1ChartDatas.Clear();
            D1ChartDatas.Add(new NumricChartData1D("Parameter", "扫描结果", ChartDataType.X));
            D1ChartDatas.Add(new NumricChartData1D("Signal", "扫描结果", ChartDataType.Y));

            // 获取数据源引用
            var xs = Get1DChartDataSource("Parameter", "扫描结果");
            var ys = Get1DChartDataSource("Signal", "扫描结果");

            for (int i = 0; i < scanPoints; i++)
            {
                if (IsExpResume) break;

                double x = scanPoints > 1
                    ? startFreq + (endFreq - startFreq) * i / (scanPoints - 1)
                    : startFreq;
                double y = Math.Sin(x) * 100;

                // 分别添加 X 和 Y 值
                xs.Add(x);
                ys.Add(y);

                UpdatePlotChartFlow();
                SetExpState(string.Format("扫描点 {0}/{1}", i + 1, scanPoints));
                SetProgress((i + 1) * 100 / scanPoints);

                System.Threading.Thread.Sleep(200);
            }

            SetExpState("扫描完成");
        }

        public override void AfterExpEvent()
        {
        }
    }
}

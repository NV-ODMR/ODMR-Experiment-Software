using System;
using System.Collections.Generic;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;

namespace AI动态测试
{
    public class MultiChartTest : ODMRExpObject
    {
        public override string ODMRExperimentName => "多图表测试";
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
            SetExpState("多图表测试开始");
            
            // 创建多个图表
            var chart1 = new D1ChartData("正弦波", "图表1");
            var chart2 = new D1ChartData("余弦波", "图表2");
            var chart3 = new D1ChartData("指数衰减", "图表3");
            
            _d1ChartDatas.Add(chart1);
            _d1ChartDatas.Add(chart2);
            _d1ChartDatas.Add(chart3);
            
            // 同时填充数据
            for (int i = 0; i < 20; i++)
            {
                if (IsExpResume) break;
                
                double x = i * 0.5;
                double y1 = Math.Sin(x) * 50;
                double y2 = Math.Cos(x) * 50;
                double y3 = 100 * Math.Exp(-x / 5.0);
                
                chart1.D1Data.Add(new D1DataPoint(x, y1));
                chart2.D1Data.Add(new D1DataPoint(x, y2));
                chart3.D1Data.Add(new D1DataPoint(x, y3));
                
                SetExpState(string.Format("数据点 {0}/20", i + 1));
                SetProgress((i + 1) * 5);
                
                System.Threading.Thread.Sleep(150);
            }
            
            SetExpState("多图表测试完成");
        }

        public override void AfterExpEvent()
        {
        }

        protected override void InnerRead(FileObject fobj)
        {
        }

        protected override void InnerWrite(FileObject obj)
        {
        }

        protected override void InnerToDataVisualSource(DataVisualSource source)
        {
        }
    }
}

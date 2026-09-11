
using System;
using System.Collections.Generic;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;

namespace AI动态实验
{
    public class SimpleTestExp : ODMRExpObject
    {
        public override string ODMRExperimentName => "简单测试实验";
        public override string ODMRExperimentGroupName => "AI测试";

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
            SetExpState("测试实验运行中");
            System.Threading.Thread.Sleep(1000);
            SetExpState("测试完成");
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

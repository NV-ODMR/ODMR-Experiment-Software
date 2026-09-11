using System;
using System.Collections.Generic;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;

namespace AI动态测试
{
    public class ErrorHandlingTest : ODMRExpObject
    {
        public override string ODMRExperimentName => "错误处理测试";
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
            SetExpState("错误处理测试开始");
            
            try
            {
                // 模拟一些正常操作
                for (int i = 0; i < 5; i++)
                {
                    SetExpState(string.Format("正常步骤 {0}/5", i + 1));
                    System.Threading.Thread.Sleep(200);
                }
                
                // 故意抛出异常
                SetExpState("即将发生错误...");
                System.Threading.Thread.Sleep(500);
                
                throw new InvalidOperationException("这是一个测试异常，用于验证错误捕获和堆栈跟踪功能");
            }
            catch (Exception ex)
            {
                // 记录错误但不中断实验
                SetExpState("捕获到异常: " + ex.Message);
                System.Threading.Thread.Sleep(1000);
                
                // 继续执行
                SetExpState("错误处理后继续运行");
                System.Threading.Thread.Sleep(500);
            }
            
            SetExpState("错误处理测试完成");
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ODMR_Lab;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.ODMR实验.AI实验
{
    public class SampleCounterExp : ODMRExpObject
    {
        private int _count = 0;
        private int _maxCount = 10;

        public override string ODMRExperimentName { get; set; } = "AI计数器实验";
        public override string ODMRExperimentGroupName { get; set; } = "AI实验";
        public override string Description { get; set; } = "一个简单的计数实验，演示动态实验功能";
        public override bool Is1DScanExp { get; set; } = false;
        public override bool Is2DScanExp { get; set; } = false;
        public override bool IsAFMSubExperiment { get; protected set; } = false;

        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("最大计数值", 10, "MaxCount")
        };

        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>();

        public override List<InfoBase> DeviceList { get; set; } = new List<InfoBase>();

        public override List<ParentPlotDataPack> D1ChartDatas { get; set; } = new List<ParentPlotDataPack>();

        public override List<FittedData1D> D1FitDatas { get; set; } = new List<FittedData1D>();

        public override List<ChartData2D> D2ChartDatas { get; set; } = new List<ChartData2D>();

        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>
            {
                new KeyValuePair<string, Action>("计数+1", () => { _count++; MessageLogger.LogInfo("计数: " + _count); }),
                new KeyValuePair<string, Action>("重置计数", () => { _count = 0; MessageLogger.LogInfo("计数已重置"); })
            };
        }

        protected override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>();
        }

        protected override void PreConfirmProcedure()
        {
            _maxCount = GetInputParamValueByName("MaxCount");
            MessageLogger.LogInfo("AI计数器实验预确认，最大计数: " + _maxCount);
        }

        protected override Task<ExpRunResult> ExperimentCore(CancellationToken token)
        {
            MessageLogger.LogInfo("开始计数实验，最大计数: " + _maxCount);
            for (int i = 0; i <= _maxCount; i++)
            {
                if (token.IsCancellationRequested)
                    return Task.FromResult(ExpRunResult.Failed("用户取消"));
                _count = i;
                MessageLogger.LogInfo("当前计数: " + i);
                Thread.Sleep(500);
            }
            MessageLogger.LogInfo("实验完成！最终计数: " + _count);
            return Task.FromResult(ExpRunResult.Successful());
        }
    }
}

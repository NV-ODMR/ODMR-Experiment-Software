using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using ODMR_Lab;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.设备部分;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验类;
using ODMR_Lab.数据处理;

namespace AI动态实验
{
    /// <summary>
    /// AI动态实验参数
    /// </summary>
    public class SampleCounterParams : ExpParamBase
    {
        public int CountTarget = 100;
        public int DelayMs = 10;

        public override Dictionary<string, string> GenerateDescription()
        {
            var dict = base.GenerateDescription();
            dict["计数目标"] = CountTarget.ToString();
            dict["延迟毫秒"] = DelayMs.ToString();
            return dict;
        }

        public override void ReadFromDescription(Dictionary<string, string> descriptions)
        {
            base.ReadFromDescription(descriptions);
            if (descriptions.ContainsKey("计数目标"))
                int.TryParse(descriptions["计数目标"], out CountTarget);
            if (descriptions.ContainsKey("延迟毫秒"))
                int.TryParse(descriptions["延迟毫秒"], out DelayMs);
        }
    }

    /// <summary>
    /// AI动态实验配置
    /// </summary>
    public class SampleCounterConfig : ConfigBase
    {
        public int CountTarget;
        public int DelayMs;
    }

    /// <summary>
    /// 示例计数器实验 - 由AI动态创建
    /// 这是一个简单的计数实验，用于演示动态实验系统
    /// </summary>
    public class SampleCounterExperiment : ODMRExpObject
    {
        // ========== 实验基本信息 ==========
        private string _expName = "AI计数器实验";
        private string _groupName = "AI动态实验";

        public override string ODMRExperimentName
        {
            get { return _expName; }
            protected set { _expName = value; }
        }

        public override string ODMRExperimentGroupName
        {
            get { return _groupName; }
            protected set { _groupName = value; }
        }

        // ========== 实验类型 ==========
        private ExperimentFileTypes _expType = ExperimentFileTypes.C1DData;

        public override ExperimentFileTypes ExpType
        {
            get { return _expType; }
            protected set { _expType = value; }
        }

        // ========== 参数和配置 ==========
        private SampleCounterParams _param = new SampleCounterParams();
        private SampleCounterConfig _config = new SampleCounterConfig();

        public override ExpParamBase Param
        {
            get { return _param; }
            set { _param = value as SampleCounterParams; }
        }

        public override ConfigBase Config
        {
            get { return _config; }
            set { _config = value as SampleCounterConfig; }
        }

        public override ExpParamBase ReadParam()
        {
            return _param;
        }

        public override ConfigBase ReadConfig()
        {
            var config = new SampleCounterConfig
            {
                CountTarget = _param.CountTarget,
                DelayMs = _param.DelayMs
            };
            return config;
        }

        // ========== 输入输出参数 ==========
        private List<ExpParamBase> _inputParams;
        private List<ExpParamBase> _outputParams;

        public override List<ExpParamBase> InputParams
        {
            get
            {
                if (_inputParams == null)
                {
                    _inputParams = new List<ExpParamBase>();
                    _inputParams.Add(new IntExpParam("计数目标", 100, 1, 10000));
                    _inputParams.Add(new IntExpParam("延迟(ms)", 10, 0, 1000));
                }
                return _inputParams;
            }
            set { _inputParams = value; }
        }

        public override List<ExpParamBase> OutputParams
        {
            get
            {
                if (_outputParams == null)
                {
                    _outputParams = new List<ExpParamBase>();
                }
                return _outputParams;
            }
            set { _outputParams = value; }
        }

        // ========== 设备列表 ==========
        public override List<DeviceInfo> DeviceList
        {
            get { return new List<DeviceInfo>(); }
        }

        // ========== 图表数据 ==========
        private List<D1ChartData> _d1ChartDatas;
        private List<D1FitData> _d1FitDatas;
        private List<D2ChartData> _d2ChartDatas;

        public override List<D1ChartData> D1ChartDatas
        {
            get
            {
                if (_d1ChartDatas == null)
                {
                    _d1ChartDatas = new List<D1ChartData>();
                    _d1ChartDatas.Add(new D1ChartData("计数进度", "计数值", "序号"));
                }
                return _d1ChartDatas;
            }
        }

        public override List<D1FitData> D1FitDatas
        {
            get
            {
                if (_d1FitDatas == null)
                {
                    _d1FitDatas = new List<D1FitData>();
                }
                return _d1FitDatas;
            }
        }

        public override List<D2ChartData> D2ChartDatas
        {
            get
            {
                if (_d2ChartDatas == null)
                {
                    _d2ChartDatas = new List<D2ChartData>();
                }
                return _d2ChartDatas;
            }
        }

        // ========== 交互按钮 ==========
        protected override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        // ========== 实验生命周期 ==========
        public override bool PreConfirmProcedure()
        {
            // 实验前确认，这里直接返回true
            return true;
        }

        public override List<InfoBase> GetDevices()
        {
            // 此实验不需要设备
            return new List<InfoBase>();
        }

        public override void PreExpEvent()
        {
            // 实验开始前调用
            SetExpState("准备开始计数实验...");
        }

        protected override void ExperimentCore()
        {
            // 核心实验逻辑
            var config = _config;
            int target = config.CountTarget;
            int delay = config.DelayMs;

            SetExpState("正在计数...");

            // 获取图表数据
            var chartData = D1ChartDatas[0];
            chartData.Clear();

            double[] xData = new double[target];
            double[] yData = new double[target];

            for (int i = 0; i < target; i++)
            {
                // 检查是否被停止
                JudgeThreadEndOrResumeAction();

                // 模拟计数工作
                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }

                xData[i] = i + 1;
                yData[i] = i + 1;

                // 更新进度
                SetProgress((double)(i + 1) / target * 100);
                SetExpState("计数: " + (i + 1) + " / " + target);

                // 更新图表（每10个点更新一次）
                if ((i + 1) % 10 == 0 || i == target - 1)
                {
                    double[] xSub = new double[i + 1];
                    double[] ySub = new double[i + 1];
                    Array.Copy(xData, xSub, i + 1);
                    Array.Copy(yData, ySub, i + 1);
                    chartData.SetExpData(xSub, ySub, "计数");
                }
            }

            SetExpState("实验完成");
        }

        public override void AfterExpEvent()
        {
            // 实验结束后调用
            SetExpState("实验已结束");
        }

        // ========== 文件读写 ==========
        protected override void InnerRead(FileObject fobj)
        {
            // 从文件读取额外数据
        }

        protected override void InnerWrite(FileObject obj)
        {
            // 写入额外数据到文件
        }

        protected override void InnerToDataVisualSource(DataVisualSource source)
        {
            // 转换为数据可视化源
        }
    }
}

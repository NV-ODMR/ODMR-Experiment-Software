using System;
using System.Collections.Generic;
using ODMR_Lab.实验部分.ODMR实验.参数;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;

namespace ODMRLab.AI.DynamicExperiments.SampleExperiments
{
    /// <summary>
    /// 示例 AI 实验 - 简单的计数器实验
    /// </summary>
    public class SampleCounterExperiment : ODMRExpObject
    {
        public override string Description 
        { 
            get => "示例 AI 实验：计数器"; 
            set { } 
        }

        public override bool IsAFMSubExperiment 
        { 
            get => false; 
            protected set { } 
        }

        public override bool Is1DScanExp 
        { 
            get => false; 
            set { } 
        }

        public override bool Is2DScanExp 
        { 
            get => false; 
            set { } 
        }

        public override List<ODMRExpObject> GetSubExperiments()
        {
            return new List<ODMRExpObject>();
        }

        public override List<KeyValuePair<string, Action>> AddInteractiveButtons()
        {
            return new List<KeyValuePair<string, Action>>();
        }

        public override void PreExpEvent()
        {
            // 实验前准备
        }

        public override void AfterExpEvent()
        {
            // 实验后处理
        }

        public override void ODMRExperiment()
        {
            // 简单的计数实验
            int count = 0;
            for (int i = 0; i < 10; i++)
            {
                count += i;
                System.Threading.Thread.Sleep(100); // 模拟实验过程
            }
            
            // 设置输出参数
            SetOutputParamByName("Count", count.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.IO操作;
using ODMR_Lab.基本控件;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.设备部分;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.线扫描
{
    class T1 : Scan1DExpBase<object>
    {
        public override string ODMRExperimentName { get; set; } = "连续波谱(CW)";
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            //0.Pi脉冲长度(整数),1.T1间隔长度(整数),2.采样循环次数(整数)，3.超时时间（整数）4.微波频率（小数）5.微波功率（小数）
            new Param<int>("Pi脉冲长度(ns)",20,"Pi"),
            new Param<int>("T1最小值(ns)",20,"T1min"),
            new Param<int>("T1最大值(ns)",100,"T1max"),
            new Param<int>("T1点数(ns)",20,"T1points"),
            new Param<int>("采样循环次数",1000,"LoopCount"),
            new Param<int>("超时时间(ms)",100000,"TimeMax"),
            new Param<double>("微波频率(MHz)",2870,"RFFrequency"),
            new Param<double>("微波功率(dBm)",-20,"RFAmplitude")
        };
        public override List<ParamB> OutputParams { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override List<ChartData1D> D1ChartDatas { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override List<ChartData2D> D2ChartDatas { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void AfterScanEvent()
        {
            throw new NotImplementedException();
        }

        public override string CreateThreadState(object dev, double currentloc)
        {
            throw new NotImplementedException();
        }

        public override List<object> FirstScanEvent(object device, ScanRange range, double locvalue, List<object> inputParams)
        {
            throw new NotImplementedException();
        }

        public override KeyValuePair<ScanRange, bool> GetScanRange()
        {
            throw new NotImplementedException();
        }

        public override object GetScanSource()
        {
            throw new NotImplementedException();
        }

        public override bool PreConfirmProcedure()
        {
            throw new NotImplementedException();
        }

        public override void PreScanEvent()
        {
            throw new NotImplementedException();
        }

        public override List<object> ScanEvent(object device, ScanRange range, double locvalue, List<object> inputParams)
        {
            throw new NotImplementedException();
        }
    }
}

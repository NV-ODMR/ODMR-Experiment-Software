using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.数据处理;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.点扫描
{
    public class Ramsey : ODMRExpObject
    {
        #region 实验部分
        public override List<ParamB> InputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("测试1",1,"Test1"),
            new Param<int>("测试2",1,"Test2"),
            new Param<int>("测试3",1,"Test3"),
        };
        public override List<ParamB> OutputParams { get; set; } = new List<ParamB>()
        {
            new Param<int>("测试1",1,"Test1"),
            new Param<int>("测试2",1,"Test2"),
            new Param<int>("测试3",1,"Test3"),
        };

        public override List<KeyValuePair<DeviceTypes, Param<string>>> DeviceList { get; set; } = new List<KeyValuePair<DeviceTypes, Param<string>>>()
        {
            new KeyValuePair<DeviceTypes, Param<string>>(DeviceTypes.相机,new Param<string>("相机","","Cam"))
        };

        public override ExpParamBase Param { get; set; } = null;
        public override ConfigBase Config { get; set; } = null;
        public override string ODMRExperimentName { get; set; } = "单点Ramsey";

        public override void ExperimentEvent()
        {
            int ind = 0;
            while (true)
            {
                SetExpState("正在循环,当前次数:" + ind.ToString());
                Thread.Sleep(3000);
                ++ind;
            }
        }

        public override List<InfoBase> GetDevices()
        {
            return new List<InfoBase>();
        }

        public override bool PreConfirmProcedure()
        {
            return true;
        }

        protected override void InnerRead(FileObject fobj)
        {
            return;
        }

        #endregion

        #region 数据部分
        protected override void InnerWrite(FileObject obj)
        {
            return;
        }

        public override void SetDataToDataVisualSource(DataVisualSource source)
        {
        }
        #endregion
    }
}

using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.扫描基方法;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM.二维扫描
{
    public abstract class Scan2DExpBase<T1, T2> : ODMRExperimentWithAFM
    {
        Scan2DSession<T1, T2> D2Session = new Scan2DSession<T1, T2>();

        public override void ODMRExpWithAFM()
        {
            T1 dev1 = GetScanSource1();
            T2 dev2 = GetScanSource2();

            ScanRange range1 = GetScanRange1();
            ScanRange range2 = GetScanRange2();

            D2Session.FirstScanEvent = FirstScanEvent;
            D2Session.ScanEvent = ScanEvent;
            D2Session.ScanSource1 = dev1;
            D2Session.ScanSource2 = dev2;
            D2Session.ProgressBarMethod = new Action<T1, T2, double>((devi1, devi2, val) =>
            {
                SetProgress(val);
            });
            D2Session.SetStateMethod = new Action<T1, T2, double, double>((devi1, devi2, val1, val2) =>
            {
                SetExpState(CreateThreadState(devi1, devi2, val1, val2));
            });
            D2Session.StateJudgeEvent = JudgeThreadEndOrResume;
            D2Session.BeginScan(range1, range2, 0, 100);
        }

        /// <summary>
        /// 设置轴1扫描范围（扫描范围，是否反向）
        /// </summary>
        /// <returns></returns>
        public abstract ScanRange GetScanRange1();

        /// <summary>
        /// 设置轴2扫描范围（扫描范围，是否反向）
        /// </summary>
        /// <returns></returns>
        public abstract ScanRange GetScanRange2();

        /// <summary>
        /// 设置扫描源1
        /// </summary>
        /// <returns></returns>
        public abstract T1 GetScanSource1();

        /// <summary>
        /// 设置扫描源2
        /// </summary>
        /// <returns></returns>
        public abstract T2 GetScanSource2();

        /// <summary>
        /// 设置当前状态描述
        /// </summary>
        /// <returns></returns>
        public abstract string CreateThreadState(T1 dev1, T2 dev2, double currentvalue1, double currentvalue2);

        /// <summary>
        /// 扫描操作，inputParams为空
        /// </summary>
        /// <param name="device"></param>
        /// <param name="locvalue"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public abstract List<object> ScanEvent(T1 device1, T2 device2, ScanRange range1, ScanRange range2, double loc1value, double loc2value, List<object> inputParams);

        /// <summary>
        /// 第一次扫描操作，inputParams为空
        /// </summary>
        /// <param name="device"></param>
        /// <param name="locvalue"></param>
        /// <param name="inputParams"></param>
        /// <returns></returns>
        public abstract List<object> FirstScanEvent(T1 device1, T2 device2, ScanRange range1, ScanRange range2, double loc1value, double loc2value, List<object> inputParams);
    }
}

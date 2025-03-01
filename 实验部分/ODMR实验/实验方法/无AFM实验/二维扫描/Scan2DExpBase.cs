using Controls.Windows;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.扫描基方法;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.二维扫描
{
    public abstract class Scan2DExpBase<T1, T2> : ODMRExperimentWithoutAFM
    {
        CustomScan2DSession<T1, T2> D2Session = new CustomScan2DSession<T1, T2>();

        public override void ODMRExpWithoutAFM()
        {
            if (D2ScanRange == null) throw new Exception("没有设置扫描方式");

            T1 dev1 = GetScanSource1();
            T2 dev2 = GetScanSource2();

            D2Session.FirstScanEvent = FirstScanEvent;
            D2Session.ScanEvent = ScanEvent;
            D2Session.StartScanNewLineEvent = StartScanNewLineEvent;
            D2Session.EndScanNewLineEvent = EndScanNewLineEvent;
            D2Session.ScanSource1 = dev1;
            D2Session.ScanSource2 = dev2;
            D2Session.ProgressBarMethod = new Action<T1, T2, double>((devi1, devi2, val) =>
            {
                SetProgress(val);
            });
            D2Session.SetStateMethod = new Action<T1, T2, Point>((devi1, devi2, p) =>
            {
                SetExpState(CreateThreadState(devi1, devi2, p));
            });
            D2Session.StateJudgeEvent = JudgeThreadEndOrResume;

            D2Session.BeginScan(D2ScanRange, 0, 100);
        }

        /// <summary>
        /// 每行最后一个点的扫描方法
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="base"></param>
        /// <param name="point"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected abstract List<object> EndScanNewLineEvent(T1 t1, T2 t2, D2ScanRangeBase @base, Point point, List<object> list);

        /// <summary>
        /// 每行第一个点的扫描方法
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="base"></param>
        /// <param name="point"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        protected abstract List<object> StartScanNewLineEvent(T1 t1, T2 t2, D2ScanRangeBase @base, Point point, List<object> list);

        /// <summary>
        /// 普通点的扫描方法
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <param name="range"></param>
        /// <param name="point"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        protected abstract List<object> ScanEvent(T1 t1, T2 t2, D2ScanRangeBase range, Point point, List<object> list2);

        protected abstract List<object> FirstScanEvent(T1 t1, T2 t2, D2ScanRangeBase range, Point point, List<object> list2);

        protected D2ScanRangeBase GetScanRange()
        {
            return D2ScanRange;
        }

        public override void PreExpEventWithoutAFM()
        {
            //检查扫描范围
            if (D2ScanRange == null) throw new Exception("扫描范围未设置");
            Preview2DScanEventWithoutAFM();
        }

        /// <summary>
        /// 二维扫描开始前需要进行的操作
        /// </summary>
        protected abstract void Preview2DScanEventWithoutAFM();

        public override void AfterExpEventWithoutAFM()
        {
            After2DScanEventWithoutAFM();
        }
        /// <summary>
        /// 二维扫描完成后需要进行的操作
        /// </summary>
        protected abstract void After2DScanEventWithoutAFM();

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
        public abstract string CreateThreadState(T1 dev1, T2 dev2, Point scanpoint);

    }
}

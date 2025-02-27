using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法
{
    /// <summary>
    /// 自定义点列表二维扫描方法
    /// </summary>
    internal class CustomScan2DSession<T1, T2> : ScanHelper
    {
        /// <summary>
        /// 目标源1
        /// </summary>
        public T1 ScanSource1 { get; set; }

        /// <summary>
        /// 目标源2
        /// </summary>
        public T2 ScanSource2 { get; set; }

        /// <summary>
        /// 进度条设置操作（设备，当前进度）
        /// </summary>
        public Action<T1, T2, double> ProgressBarMethod = null;

        /// <summary>
        /// 设置状态操作（设备1,设备2,当前扫描点）
        /// </summary>
        public Action<T1, T2, Point> SetStateMethod = null;

        /// <summary>
        /// 扫描第一个点时进行的操作(源设备,扫描点列表,扫描点,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T1, T2, D2ScanRangeBase, Point, List<object>, List<object>> FirstScanEvent = null;

        /// <summary>
        /// 扫描第一个点时进行的操作(源设备,扫描点列表,扫描点,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T1, T2, D2ScanRangeBase, Point, List<object>, List<object>> ScanEvent = null;

        /// <summary>
        /// 扫描新行第一个点时进行的操作(源设备,扫描点列表,扫描点,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T1, T2, D2ScanRangeBase, Point, List<object>, List<object>> StartScanNewLineEvent = null;

        /// <summary>
        /// 扫描新行最后一个点时进行的操作(源设备,扫描点列表,扫描点,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T1, T2, D2ScanRangeBase, Point, List<object>, List<object>> EndScanNewLineEvent = null;

        /// <summary>
        /// 状态判断事件,此事件用来决定是否退出扫描步骤
        /// </summary>
        public Action StateJudgeEvent = null;


        /// <summary>
        /// 开始扫描(阻塞),ScanEvent输入参数为ps,返回值为最后一次执行ScanEvent得到的输出
        /// </summary>
        /// <param name="Scandir"></param>
        /// <param name="Lo"></param>
        /// <param name="Hi"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public List<object> BeginScan(D2ScanRangeBase range, double progressLo = 0, double progressHi = 100, params object[] ps)
        {
            SetProgress(progressLo);

            var progress = new D1LinearScanRange(progressLo, progressHi, range.ScanPoints.Count, false).ScanPoints;

            bool IsFirstScan = true;

            List<object> result = null;

            try
            {
                var points = range.ScanPoints.Select(x => x.LocPoint).ToList();
                for (int i = 0; i < range.ScanPoints.Count; i++)
                {
                    if (IsFirstScan)
                    {
                        result = FirstScanEvent?.Invoke(ScanSource1, ScanSource2, range, range.ScanPoints[i].LocPoint, ps.ToList());
                        SetProgress(progress[i]);
                        SetStateMethod?.Invoke(ScanSource1, ScanSource2, range.ScanPoints[i].LocPoint);
                        IsFirstScan = false;
                    }
                    else
                    {
                        if (range.ScanPoints[i].Tag != range.ScanPoints[i - 1].Tag)
                        {
                            result = StartScanNewLineEvent?.Invoke(ScanSource1, ScanSource2, range, range.ScanPoints[i].LocPoint, ps.ToList());
                            SetProgress(progress[i]);
                            SetStateMethod?.Invoke(ScanSource1, ScanSource2, range.ScanPoints[i].LocPoint);
                            continue;
                        }
                        if (i == range.ScanPoints.Count - 1 || range.ScanPoints[i].Tag != range.ScanPoints[i + 1].Tag)
                        {
                            result = EndScanNewLineEvent?.Invoke(ScanSource1, ScanSource2, range, range.ScanPoints[i].LocPoint, ps.ToList());
                            SetProgress(progress[i]);
                            SetStateMethod?.Invoke(ScanSource1, ScanSource2, range.ScanPoints[i].LocPoint);
                            continue;
                        }
                        result = ScanEvent?.Invoke(ScanSource1, ScanSource2, range, range.ScanPoints[i].LocPoint, ps.ToList());
                        SetProgress(progress[i]);
                        SetStateMethod?.Invoke(ScanSource1, ScanSource2, range.ScanPoints[i].LocPoint);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SetProgress(double v)
        {
            ProgressBarMethod?.Invoke(ScanSource1, ScanSource2, v);
        }
    }
}

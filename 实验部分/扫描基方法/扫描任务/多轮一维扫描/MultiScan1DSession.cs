using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.实验部分.扫描基方法.扫描范围;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描.数据处理方法;
using ODMR_Lab.ODMR实验;

namespace ODMR_Lab.实验部分.扫描基方法.扫描任务.多轮一维扫描
{
    internal class MultiScan1DSession<T> : ScanSessionBase
    {
        /// <summary>
        /// 目标位移台
        /// </summary>
        public T ScanSource { get; set; }

        /// <summary>
        /// 进度条设置操作（设备，当前进度）
        /// </summary>
        public Action<T, double> ProgressBarMethod = null;

        /// <summary>
        /// 设置状态操作（设备,位置）
        /// </summary>
        public Action<T, int, double> SetStateMethod = null;

        /// <summary>
        /// 单次扫描数据
        /// </summary>
        private List<Tuple<string, string, double, MultiLoopDataProcessBase>> SingleScanData = new List<Tuple<string, string, double, MultiLoopDataProcessBase>>();

        public List<MultiLoopScanData> ScanData = new List<MultiLoopScanData>();


        /// <summary>
        /// 扫描第一个点时进行的操作(源设备,扫描范围,值,当前轮数,需要填充的数据列表,输入参数,返回经过处理后的输入参数)
        /// 需要填充的数据列表指：在扫描方法中会通过计算产生一系列数据,将数据添加到需要填充的数据列表中，参数分别为：数据名称，数据组名称，数据值,数据处理方法
        /// </summary>
        public Func<T, D1NumricScanRangeBase, double, int, List<Tuple<string, string, double, MultiLoopDataProcessBase>>, List<object>, List<object>> FirstScanEvent = null;

        /// <summary>
        /// 扫描其他点时进行的操作(源设备,扫描范围,值,当前轮数,需要填充的数据列表,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T, D1NumricScanRangeBase, double, int, List<Tuple<string, string, double, MultiLoopDataProcessBase>>, List<object>, List<object>> ScanEvent = null;

        /// <summary>
        /// 状态判断事件,此事件用来决定是否退出扫描步骤
        /// </summary>
        public Action StateJudgeEvent = null;

        /// <summary>
        /// 一轮扫描结束后触发的事件(当前轮数)
        /// </summary>
        public Action<int> LoopEndEvent = null;

        /// <summary>
        /// 每点实验之后更新绘图事件(参数：扫描数据集)
        /// </summary>
        public Action<List<MultiLoopScanData>> PlotEvent = null;

        /// <summary>
        /// 开始扫描(阻塞),ScanEvent输入参数为ps,返回值为最后一次执行ScanEvent得到的输出
        /// </summary>
        /// <param name="Scandir"></param>
        /// <param name="Lo"></param>
        /// <param name="Hi"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public List<object> BeginScan(int ScanLoopCount, MultiScanType ScanType, D1NumricScanRangeBase singlescanrange, double progressLo = 0, double progressHi = 100, params object[] ps)
        {
            SetProgress(progressLo);

            var scanlist = singlescanrange.ScanPoints.ToArray().ToList();

            var progress = Enumerable.Range(0, singlescanrange.ScanPoints.Count * ScanLoopCount).Select(x => progressLo + (progressHi - progressLo) * x / (singlescanrange.ScanPoints.Count * ScanLoopCount - 1)).ToList();

            bool IsFirstScan = true;

            List<object> result = null;

            for (int k = 0; k < ScanLoopCount; ++k)
            {
                if (ScanType == MultiScanType.正向扫描)
                {
                    //保持不变
                }
                if (ScanType == MultiScanType.正反向扫描)
                {
                    if (k != 0)
                        scanlist.Reverse();
                }
                if (ScanType == MultiScanType.随机扫描)
                {
                    scanlist = BlurList(scanlist.ToArray().ToList());
                }
                #region 单点操作
                try
                {
                    int ind = 0;
                    for (int i = 0; i < scanlist.Count; i++)
                    {
                        //进行操作
                        if (IsFirstScan == true)
                        {
                            SingleScanData.Clear();
                            result = FirstScanEvent?.Invoke(ScanSource, singlescanrange, scanlist[i], k, SingleScanData, ps.ToList());
                            //重新排列数组
                            int firstind = singlescanrange.GetNearestFormalIndex(scanlist[i]);
                            foreach (var item in SingleScanData)
                            {
                                MultiLoopScanData.SetValue(ScanData, k, singlescanrange.Counts, item.Item1, item.Item2, firstind, item.Item3, item.Item4);
                            }

                            PlotEvent?.Invoke(ScanData);

                            StateJudgeEvent?.Invoke();
                            IsFirstScan = false;
                            ++ind;
                            continue;
                        }
                        SingleScanData.Clear();
                        result = ScanEvent?.Invoke(ScanSource, singlescanrange, scanlist[i], k, SingleScanData, result);
                        //重新排列数组
                        int index = singlescanrange.GetNearestFormalIndex(scanlist[i]);
                        foreach (var item in SingleScanData)
                        {
                            MultiLoopScanData.SetValue(ScanData, k, singlescanrange.Counts, item.Item1, item.Item2, index, item.Item3, item.Item4);
                        }

                        PlotEvent?.Invoke(ScanData);

                        StateJudgeEvent?.Invoke();
                        SetProgress(progress[ind]);
                        SetStateMethod?.Invoke(ScanSource, k, scanlist[i]);
                        ++ind;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                StateJudgeEvent?.Invoke();
                //每轮操作结束后进行的操作
                LoopEndEvent?.Invoke(k);
                StateJudgeEvent?.Invoke();
                #endregion
            }

            return result;
        }

        private static Random rand = new Random();
        /// <summary>
        /// 打乱数组
        /// </summary>
        /// <returns></returns>
        private List<double> BlurList(List<double> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                double temp = list[i];
                int ind = rand.Next(list.Count);
                list[i] = list[ind];
                list[ind] = temp;
            }
            return list;
        }

        private void SetProgress(double v)
        {
            ProgressBarMethod?.Invoke(ScanSource, v);
        }
    }
}

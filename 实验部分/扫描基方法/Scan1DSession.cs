using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Threading;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;
using ODMR_Lab.设备部分.位移台部分;

namespace ODMR_Lab.实验部分.扫描基方法
{
    internal class Scan1DSession<T> : ScanHelper
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
        public Action<T, double> SetStateMethod = null;

        /// <summary>
        /// 扫描第一个点时进行的操作(源设备,扫描范围,值,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T, ScanRange, double, List<object>, List<object>> FirstScanEvent = null;

        /// <summary>
        /// 扫描其他点时进行的操作(源设备,扫描范围,值,输入参数,返回经过处理后的输入参数)
        /// </summary>
        public Func<T, ScanRange, double, List<object>, List<object>> ScanEvent = null;

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
        public List<object> BeginScan(ScanRange range, double progressLo = 0, double progressHi = 100, params object[] ps)
        {
            SetProgress(progressLo);

            var scanlist = range.GenerateScanList();

            var progress = new ScanRange(progressLo, progressHi, range.Count).GenerateScanList();


            bool IsFirstScan = true;

            List<object> result = null;

            try
            {
                int ind = 0;
                for (int i = 0; i < scanlist.Count; i++)
                {
                    //进行操作
                    if (IsFirstScan == true)
                    {
                        result = FirstScanEvent?.Invoke(ScanSource, range, scanlist[i], ps.ToList());
                        StateJudgeEvent?.Invoke();
                        IsFirstScan = false;
                        ++ind;
                        continue;
                    }
                    result = ScanEvent?.Invoke(ScanSource, range, scanlist[i], result);
                    StateJudgeEvent?.Invoke();
                    SetProgress(progress[ind]);
                    SetStateMethod?.Invoke(ScanSource, scanlist[i]);
                    ++ind;
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
            ProgressBarMethod?.Invoke(ScanSource, v);
        }
    }
}

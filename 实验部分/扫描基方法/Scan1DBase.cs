using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Threading;
using ODMR_Lab.位移台部分;
using ODMR_Lab.实验部分.磁场调节;
using System.Threading;
using System.Windows;

namespace ODMR_Lab.实验部分.扫描基方法
{
    internal class Scan1DSession : ScanHelper
    {
        /// <summary>
        /// 目标位移台
        /// </summary>
        public NanoStageInfo ScanMover { get; set; } = null;

        /// <summary>
        /// 进度条设置操作
        /// </summary>
        public Action<NanoStageInfo, double> ProgressBarMethod = null;

        /// <summary>
        /// 扫描第一个点时进行的操作
        /// </summary>
        public ScanHelper.ScanOperation FirstScanEvent = null;

        /// <summary>
        /// 扫描其他点时进行的操作
        /// </summary>
        public ScanHelper.ScanOperation ScanEvent = null;

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
        public List<object> BeginScan(double Lo, double Hi, double RestrictLo, double RestrictHi, int pointscount, double moveMaxStep = 0.1, double progressLo = 0, double progressHi = 100, params object[] ps)
        {
            SetProgress(progressLo);

            double step = (Hi - Lo) / (pointscount - 1);
            if (double.IsNaN(step)) step = Hi - Lo;

            bool IsFirstScan = true;

            List<object> result = null;

            //bool res = ScanMover.Use();
            //if (res == false)
            //{
            //    throw new Exception(ScanMover.Parent.Device.ProductName + "的" + ScanMover.Device.AxisName + "轴被占用");
            //}

            try
            {
                for (int i = 0; i < pointscount; i++)
                {
                    //移动位移台
                    MoveWithoutUse(ScanMover, StateJudgeEvent, RestrictLo, RestrictHi, Lo + step * i, 10000, moveMaxStep);
                    //进行操作
                    if (IsFirstScan == true)
                    {
                        result = FirstScanEvent?.Invoke(ScanMover, Lo + step * i, ps.ToList());
                        StateJudgeEvent?.Invoke();
                        IsFirstScan = false;
                        continue;
                    }
                    result = ScanEvent?.Invoke(ScanMover, Lo + step * i, result);
                    StateJudgeEvent?.Invoke();
                    SetProgress(progressLo + (i + 1) * (progressHi - progressLo) / pointscount);
                    Thread.Sleep(500);
                }
                ScanMover?.UnUse();
                return result;
            }
            catch (Exception ex)
            {
                ScanMover?.UnUse();
                throw ex;
            }
        }


        private void SetProgress(double v)
        {
            ProgressBarMethod?.Invoke(ScanMover, v);
        }
    }
}

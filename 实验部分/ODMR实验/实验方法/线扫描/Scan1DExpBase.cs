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
using ODMR_Lab.设备部分;
using ODMR_Lab.设备部分;
using ODMR_Lab.ODMR实验;
using HardWares.端口基类;
using ODMR_Lab.实验部分.扫描基方法;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.线扫描
{
    internal abstract class Scan1DExpBase : SequenceExpObject
    {
        /// <summary>
        /// 一维扫描设备
        /// </summary>
        public PortObject ScanSource { get; set; } = null;

        /// <summary>
        /// 扫描前的进度条值
        /// </summary>
        protected abstract double ProgressBeforeScan { get; set; }

        /// <summary>
        /// 扫描后的进度条值
        /// </summary>
        protected abstract double ProgressAfterScan { get; set; }



        public override void ExperimentEvent()
        {
            Scan1DSession ScanSe = new Scan1DSession();
            ScanSe.ProgressBarMethod = new Action<NanoStageInfo, double>(() =>
              {

              });
            ScanSe.BeginScan();
        }

        private void SetProgress(double v)
        {
            ProgressBarMethod?.Invoke(ScanMover, v);
        }
    }
}

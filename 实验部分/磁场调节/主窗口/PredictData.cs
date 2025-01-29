using ODMR_Lab.磁场调节;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.磁场调节.主窗口
{
    public class PredictData
    {
        public MagnetScanExpParams Params { get; set; } = null;

        public MagnetScanConfigParams ConfigParams { get; set; } = null;

        /// <summary>
        /// 要预测的角度参数
        /// </summary>
        public double ThetaPredictInput { get; set; } = double.NaN;

        /// <summary>
        /// 要预测的角度参数
        /// </summary>
        public double PhiPredictInput { get; set; } = double.NaN;

        /// <summary>
        /// 要预测的角度参数
        /// </summary>
        public double ZPredictHeightInput { get; set; } = double.NaN;

        /// <summary>
        /// 预测的X位置
        /// </summary>
        public double XLocPredictOutPut { get; set; } = double.NaN;
        /// <summary>
        /// 预测的Y位置
        /// </summary>
        public double YLocPredictOutPut { get; set; } = double.NaN;
        /// <summary>
        /// 预测的Z位置
        /// </summary>
        public double ZLocPredictOutPut { get; set; } = double.NaN;
        /// <summary>
        /// 预测的角度位置
        /// </summary>
        public double ALocPredictOutPut { get; set; } = double.NaN;

        /// <summary>
        /// 预测的磁场强度
        /// </summary>
        public double PredictB { get; set; } = double.NaN;

        public PredictData(MagnetScanExpParams P, MagnetScanConfigParams CP, double Ptheta, double Pphi, double Pheight)
        {
            Params = P; ConfigParams = CP;
            ThetaPredictInput = Ptheta;
            PhiPredictInput = Pphi;
            ZPredictHeightInput = Pheight;
        }
    }
}

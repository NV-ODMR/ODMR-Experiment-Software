
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.其他
{
    public class PredictData
    {
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

        public double XLoc { get; private set; }
        public double YLoc { get; private set; }
        public double ZLoc { get; private set; }

        public double ZDistance { get; private set; }

        public double NVTheta { get; private set; }
        public double NVPhi { get; private set; }

        public PredictData(double XLoc, double YLoc, double ZLoc, double ZDistance, double NVTheta, double NVPhi, double Ptheta, double Pphi, double Pheight)
        {
            ThetaPredictInput = Ptheta;
            PhiPredictInput = Pphi;
            ZPredictHeightInput = Pheight;

            this.XLoc = XLoc;
            this.YLoc = YLoc;
            this.ZLoc = ZLoc;
            this.NVTheta = NVTheta;
            this.NVPhi = NVPhi;
            this.ZDistance = ZDistance;
        }
    }
}

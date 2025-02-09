using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PythonHandler;
using MathNet.Numerics;

namespace ODMR_Lab.Python.LbviewHandler
{
    public class LabviewConverter
    {
        /// <summary>
        /// Autotrace
        /// </summary>
        /// <param name="exc"></param>
        public static void AutoTrace(out Exception exc)
        {
            #region 测试代码，跳过此步骤
            exc = null;
            return;
            #endregion
            try
            {
                Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "LabviewHandler", "AutoTraceVI.py"), "AutoTrace", TimeSpan.FromSeconds(50));
                exc = null;
                return;
            }
            catch (Exception ex)
            {
                exc = ex;
            }

        }

        static Random r = new Random();
        /// <summary>
        /// 测NV的CW谱(只取对比度大于0.07的峰)
        /// </summary>
        public static void CW(out List<double> Frequences, out List<double> Contracts, out List<double> fitpeaks, out List<double> fitcontracts, out Exception exc, double startFreq, double endFreq, double step, int averagetime, int peakcount,
          int scanRangeAfterstop = 20, bool isReverse = false)
        {
            fitpeaks = new List<double>();
            fitcontracts = new List<double>();
            Frequences = new List<double>();
            Contracts = new List<double>();

            #region 测试代码，生成随机结果
            double cc = r.Next(0, 40);
            fitpeaks.Add(2870 - cc);
            fitpeaks.Add(2870 + cc);
            fitcontracts.Add(0.7);
            fitcontracts.Add(0.7);
            for (int i = 0; i < 10; i++)
            {
                Frequences.Add(r.NextDouble());
                Contracts.Add(r.NextDouble());
            }
            exc = null;
            return;
            #endregion

            try
            {
                dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "LabviewHandler", "CWVI.py"), "RunCWGetRawData", TimeSpan.FromSeconds(300), startFreq, endFreq, step, averagetime, peakcount, scanRangeAfterstop, isReverse);
                //限定对比度范围
                if (0.05 < result[2] < 0.4)
                {
                    fitpeaks.Add(result[0]);
                    fitcontracts.Add(result[2]);
                }
                if (0.05 < result[3] < 0.4)
                {
                    fitpeaks.Add(result[1]);
                    fitcontracts.Add(result[3]);
                }
                foreach (var item in result[4])
                {
                    Frequences.Add(item);
                }
                foreach (var item in result[5])
                {
                    Contracts.Add(item);
                }
                exc = null;
                return;
            }
            catch (Exception ex)
            {
                exc = ex;
                return;
            }
        }

        /// <summary>
        /// 初始化Labview程序
        /// </summary>
        /// <param name="exc"></param>
        public static void InitLabview(out Exception exc)
        {
            try
            {
                dynamic result = Python_NetInterpretor.ExcuteFunction(Path.Combine(Environment.CurrentDirectory, "Python", "LabviewHandler", "InitLabview.py"), "Init", TimeSpan.FromSeconds(5));
                exc = null;
                return;
            }
            catch (Exception ex)
            {
                exc = ex;
            }
        }

    }
}

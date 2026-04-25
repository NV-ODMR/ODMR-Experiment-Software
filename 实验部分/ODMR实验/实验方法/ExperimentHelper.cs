using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM.点实验.脉冲实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.实验部分.序列编辑器;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法
{
    public class ExperimentHelper
    {
        /// <summary>
        /// 获取删除掉NAN数据后的数据
        /// </summary>
        public static void GetNonNaNData(List<double> xs, List<double> ys, out List<double> outxs, out List<double> outys)
        {
            var inds = ys.Select((x, ind) => { if (!double.IsNaN(x)) return ind; else return double.NaN; }).Where((x) => !double.IsNaN(x));

            outxs = inds.Select((x) => xs[(int)x]).ToList();
            outys = inds.Select((x) => ys[(int)x]).ToList();
        }

        public static void SetLockInSequenceEvolutionPulses(double frequence, double pix, double piy, double hpix, double hpiy)
        {
            double rawevolutiontime = 1e+3 / frequence / 2;

            GlobalPulseParams.SetGlobalPulseLength("LockInSequenceDuty", (int)rawevolutiontime);
            GlobalPulseParams.SetGlobalPulseLength("RabiTime", 0);
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeX", Math.Max(20, (int)(rawevolutiontime / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeY", Math.Max(20, (int)(rawevolutiontime / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-X", Math.Max(20, (int)(rawevolutiontime - pix / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-Y", Math.Max(20, (int)(rawevolutiontime - pix / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeY-Y", Math.Max(20, (int)(rawevolutiontime - piy / 2 - piy / 2)));
        }

        /// <summary>
        /// 计算信号角频率振幅(rad/μs)
        /// </summary>
        /// <param name="sequencetype"></param>
        /// <param name="frequence"></param>
        /// <param name="pix"></param>
        /// <param name="piy"></param>
        /// <param name="hpix"></param>
        /// <param name="hpiy"></param>
        public static double CalculateSignalAmplitude(SequenceTypes sequencetype, int sequenceorder, double totalphase, double frequence, double pix, double piy, double hpix, double hpiy)
        {
            try
            {
                double rawevolutiontime = 1e+3 / frequence / 2;
                double phase_pixx = 1 / (2 * Math.PI * frequence) * (Math.Cos(Math.PI * frequence * pix / 1000) + Math.Cos(Math.PI * frequence * pix / 1000));
                double phase_pixy = 1 / (2 * Math.PI * frequence) * (Math.Cos(Math.PI * frequence * piy / 1000) + Math.Cos(Math.PI * frequence * pix / 1000));
                double phase_piyy = 1 / (2 * Math.PI * frequence) * (Math.Cos(Math.PI * frequence * piy / 1000) + Math.Cos(Math.PI * frequence * piy / 1000));
                double phase_hpix = 1 / (2 * Math.PI * frequence) * (1 - Math.Sin(Math.PI * frequence * pix / 1000));
                double unitphase = double.NaN;
                if (sequencetype == SequenceTypes.CMPG)
                {
                    unitphase = 2 * phase_hpix + (sequenceorder - 1) * phase_pixx;
                }
                if (sequencetype == SequenceTypes.XY4)
                {
                    unitphase = 2 * phase_hpix + 2 * phase_pixy + phase_piyy;
                }
                if (sequencetype == SequenceTypes.XY8)
                {
                    unitphase = 2 * phase_hpix + 6 * phase_pixy + phase_piyy;
                }
                if (sequencetype == SequenceTypes.XY8_2)
                {
                    unitphase = 2 * phase_hpix + 12 * phase_pixy + 2 * phase_piyy + phase_pixx;
                }
                if (unitphase == 0) return double.NaN;
                return totalphase / unitphase;
            }
            catch (Exception)
            {
                return double.NaN;
            }
        }

        public static void SetT2SequenceEvolutionPulses(int evotime, double pix, double piy, double hpix, double hpiy)
        {
            GlobalPulseParams.SetGlobalPulseLength("RabiTime", 0);
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeX", Math.Max(20, (int)(evotime / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeY", Math.Max(20, (int)(evotime / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-X", Math.Max(20, (int)(evotime - pix / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-Y", Math.Max(20, (int)(evotime - pix / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeY-Y", Math.Max(20, (int)(evotime - piy / 2 - piy / 2)));
        }

        public static void SetSequenceCount(SequenceDataAssemble obj, SequenceTypes sequencetype, int sequenceorder)
        {
            //传入参数为读取到的序列
            //查找X:pi脉冲的通道
            SequenceChannel ind = SequenceChannel.None;
            foreach (var ch in obj.Channels)
            {
                var pilist = ch.Peaks.Where(x => x.PeakName == "PiX");
                if (pilist.Count() != 0)
                {
                    if (pilist.ElementAt(0).IsWaveOne())
                    {
                        ind = ch.ChannelInd;
                    }
                }
            }

            if (sequencetype == SequenceTypes.CMPG)
            {
                #region 为每个通道添加对应阶数的序列
                int order = sequenceorder;
                if (order > 1)
                {
                    int det = order - 1;
                    foreach (var ch in obj.Channels)
                    {
                        ///CMPG Core 脉冲的位置
                        var CMPGS = ch.Peaks.Where((x) => x.PeakName == "CMPG-CorePulse").Select((x) => ch.Peaks.IndexOf(x)).ToList();
                        CMPGS.Sort();
                        CMPGS.Reverse();
                        foreach (var item in CMPGS)
                        {
                            var cmpgseg = ch.Peaks[item];
                            for (int i = 0; i < det; i++)
                            {
                                ch.Peaks.Insert(item + i, cmpgseg.Copy());
                            }
                        }
                    }
                }
                #endregion
            }

            double frequency = 1.0;
            int time = (int)(1.0 / frequency * 1000);

            #region 计算Delay等待时间
            var chan = obj.Channels[0];
            var peaks = obj.Channels[0].Peaks;
            if (peaks.Where((x) => x.PeakName == "Trigger_Delay序列").Count() == 0) return;
            int triggerind = peaks.IndexOf(peaks.Where((x) => x.PeakName == "Trigger_Delay序列").First());
            int countind = peaks.IndexOf(peaks.Where((x) => x.PeakName == "光子计数序列").First());
            int totalexptime = 0;
            int lighttime = peaks[countind].PeakSpan + GlobalPulseParams.GetGlobalPulseLength("LasetPolar") + GlobalPulseParams.GetGlobalPulseLength("LaserWait");
            int darktime = lighttime + GlobalPulseParams.GetGlobalPulseLength("PiX");
            lighttime = (lighttime / time + 1) * time - lighttime;
            darktime = (darktime / time + 1) * time - darktime;
            GlobalPulseParams.SetGlobalPulseLength("LightHahnechoWaitTime", lighttime);
            GlobalPulseParams.SetGlobalPulseLength("DarkhahnechoWaitTime", darktime);
            #endregion

        }
    }
}

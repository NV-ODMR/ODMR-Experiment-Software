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
            GlobalPulseParams.SetGlobalPulseLength("RabiTime", 0);
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeX", Math.Max(20, (int)(rawevolutiontime / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("HalfEvolutionTimeY", Math.Max(20, (int)(rawevolutiontime / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-X", Math.Max(20, (int)(rawevolutiontime - pix / 2 - pix / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeX-Y", Math.Max(20, (int)(rawevolutiontime - pix / 2 - piy / 2)));
            GlobalPulseParams.SetGlobalPulseLength("EvolutionTimeY-Y", Math.Max(20, (int)(rawevolutiontime - piy / 2 - piy / 2)));
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
                        ///Pi/2 脉冲的位置
                        var halfpiys = ch.Peaks.Where((x) => x.PeakName == "CustomYLength" || x.PeakName == "CustomXLength").Select((x) => ch.Peaks.IndexOf(x)).ToList();
                        if (halfpiys.Count == 0)
                        {
                            halfpiys = ch.Peaks.Where((x) => x.PeakName == "HalfPiX" || x.PeakName == "3HalfPiX").Select((x) => ch.Peaks.IndexOf(x)).Where((x) => ch.Peaks[x + 1].PeakName == "CountWait").ToList();
                        }
                        halfpiys.Sort();
                        halfpiys.Reverse();
                        foreach (var item in halfpiys)
                        {
                            List<SingleSequenceWaveSeg> segs = new List<SingleSequenceWaveSeg>();
                            for (int i = 0; i < det; i++)
                            {
                                segs.Add(new SingleSequenceWaveSeg("HalfEvolutionTimeX", GlobalPulseParams.GetGlobalPulseLength("HalfEvolutionTimeX"), WaveValues.Zero, ch));
                                segs.Add(new SingleSequenceWaveSeg("PiX", GlobalPulseParams.GetGlobalPulseLength("PiX"), (ch.ChannelInd == ind) ? WaveValues.One : WaveValues.Zero, ch));
                                segs.Add(new SingleSequenceWaveSeg("HalfEvolutionTimeX", GlobalPulseParams.GetGlobalPulseLength("HalfEvolutionTimeX"), WaveValues.Zero, ch));
                            }
                            for (int i = 0; i < segs.Count; i++)
                            {
                                ch.Peaks.Insert(item + i, segs[i]);
                            }
                        }
                    }
                }
                #endregion
            }

            #region 计算Delay等待时间
            var cha = obj.Channels[0];
            if (cha.Peaks.Where((x) => x.PeakName == "TriggerExpStartDelay").Count() == 0) return;
            int triggerind = cha.Peaks.IndexOf(cha.Peaks.Where((x) => x.PeakName == "TriggerExpStartDelay").First());
            int countind = cha.Peaks.IndexOf(cha.Peaks.Where((x) => x.PeakName == "CountWait").First());
            int totalexptime = 0;

            List<int> times = new List<int>();
            for (int i = triggerind + 1; i < countind; i++)
            {
                totalexptime += cha.Peaks[i].PeakSpan;
                times.Add(cha.Peaks[i].PeakSpan);
            }


            int lighttime = totalexptime - GlobalPulseParams.GetGlobalPulseLength("LasetPolar") - GlobalPulseParams.GetGlobalPulseLength("LaserWait");
            int darktime = lighttime - GlobalPulseParams.GetGlobalPulseLength("PiX");
            GlobalPulseParams.SetGlobalPulseLength("LightHahnechoWaitTime", lighttime);
            GlobalPulseParams.SetGlobalPulseLength("DarkhahnechoWaitTime", darktime);
            #endregion

        }
    }
}

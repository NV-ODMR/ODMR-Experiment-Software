using CodeHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验
{
    /// <summary>
    /// 全局脉冲参数
    /// </summary>
    public class GlobalPulseParams
    {
        /// <summary>
        /// 脉冲名称
        /// </summary>
        public string PulseName { get; set; } = "";

        /// <summary>
        /// 脉冲长度
        /// </summary>
        public int PulseLength { get; set; } = 20;

        public bool IsLocked { get; private set; } = true;

        public GlobalPulseParams(string pulseName, int pulseLength, bool isLocked)
        {
            PulseName = pulseName;
            PulseLength = pulseLength;
            IsLocked = isLocked;
        }

        public static List<GlobalPulseParams> GlobalPulseConfigs = new List<GlobalPulseParams>();

        public static void WriteToFile()
        {
            FileObject fobj = new FileObject();
            foreach (var item in GlobalPulseConfigs)
            {
                fobj.Descriptions.Add(item.PulseName + "→" + item.IsLocked.ToString(), item.PulseLength.ToString());
            }
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Sequences")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Sequences"));
            }
            fobj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "Sequences", "GlobalPulses.userdat"));
        }

        public static void ReadFromFile()
        {
            GlobalPulseConfigs = new List<GlobalPulseParams>();
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "Sequences", "GlobalPulses.userdat")))
                return;
            FileObject fobj = FileObject.ReadFromFile(Path.Combine(Environment.CurrentDirectory, "Sequences", "GlobalPulses.userdat"));
            foreach (var item in fobj.Descriptions)
            {
                GlobalPulseConfigs.Add(new GlobalPulseParams(item.Key.Split('→')[0], int.Parse(item.Value), bool.Parse(item.Key.Split('→')[1])));
            }
        }

        public static List<string> GetGlobalPulses()
        {
            return GlobalPulseConfigs.Select(x => x.PulseName).ToList();
        }

        /// <summary>
        /// 设置全局参数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="length"></param>
        public static void SetGlobalPulseLength(string name, int length)
        {
            var pulse = GlobalPulseConfigs.Where(x => x.PulseName == name).ToList();
            if (pulse.Count != 0)
            {
                pulse[0].PulseLength = length;
                GlobalPulseParams.WriteToFile();
            }
        }

        /// <summary>
        /// 获取全局参数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="length"></param>
        public static int GetGlobalPulseLength(string name)
        {
            var pulse = GlobalPulseConfigs.Where(x => x.PulseName == name).ToList();
            if (pulse.Count != 0)
            {
                return pulse[0].PulseLength;
            }
            throw new Exception("未找到名称为" + name + "的全局脉冲");
        }
    }
}

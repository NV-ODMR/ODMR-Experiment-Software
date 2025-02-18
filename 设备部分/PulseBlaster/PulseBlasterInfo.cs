using CodeHelper;
using HardWares.板卡;
using System.Collections.Generic;
using System.Linq;

namespace ODMR_Lab.设备部分.板卡
{
    public class PulseBlasterInfo : DeviceInfoBase<PulseBlasterBase>
    {
        public List<KeyValuePair<string, int>> ChannelDescriptions { get; set; } = new List<KeyValuePair<string, int>>();

        public override void CreateDeviceInfoBehaviour()
        {
            foreach (var item in Device.ChannelInds)
            {
                ChannelDescriptions.Add(new KeyValuePair<string, int>(item.ToString(), item));
            }
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }

        protected override void AutoConnectedAction(FileObject file)
        {
            var indexes = file.ExtractDouble("ChannelIndexes");
            var descripts = file.ExtractString("ChannelDescriptions");
            for (int i = 0; i < Device.ChannelInds.Count(); i++)
            {
                if (indexes.Contains(Device.ChannelInds.ElementAt(i)))
                {
                    int ind = indexes.IndexOf(Device.ChannelInds.ElementAt(i));
                    ChannelDescriptions.Add(new KeyValuePair<string, int>(descripts[ind], (int)indexes[ind]));
                }
                else
                {
                    ChannelDescriptions.Add(new KeyValuePair<string, int>(i.ToString(), i));
                }
            }
        }

        protected override void CloseFileAction(FileObject obj)
        {
            obj.WriteStringData("ChannelDescriptions", ChannelDescriptions.Select(x => x.Key).ToList());
            obj.WriteDoubleData("ChannelIndexes", ChannelDescriptions.Select(x => (double)x.Value).ToList());
        }

        /// <summary>
        /// 寻找指定描述对应的通道号，找不到返回-1
        /// </summary>
        /// <param name="descript"></param>
        /// <returns></returns>
        public int FindChannelOfDescription(string descript)
        {
            var res = ChannelDescriptions.Where(x => x.Key == descript);
            if (res.Count() == 0) return -1;
            return res.First().Value;
        }

        /// <summary>
        /// 寻找通道号对应的描述，找不到返回空字符串
        /// </summary>
        /// <param name="descript"></param>
        /// <returns></returns>
        public string FindDescriptionOfChannel(int channelind)
        {
            var res = ChannelDescriptions.Where(x => x.Value == channelind);
            if (res.Count() == 0) return "";
            return res.First().Key;
        }
    }
}

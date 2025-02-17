using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分
{
    public partial class DeviceDispatcher
    {
        /// <summary>
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static NanoStageInfo GetMoverDevice(MoverTypes moverType, PartTypes part)
        {
            try
            {
                if (part == PartTypes.None)
                {
                    return null;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.MoverType == moverType && item.PartType == part)
                        {
                            return item;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取位移台设备(如果设备正在使用则生成一个错误提示)
        /// </summary>
        /// <returns></returns>
        public static List<NanoStageInfo> GetMoverDevice(PartTypes part)
        {
            List<NanoStageInfo> infos = new List<NanoStageInfo>();
            try
            {
                if (part == PartTypes.None)
                {
                    return infos;
                }

                foreach (var tem in MainWindow.Dev_MoversPage.MoverList)
                {
                    foreach (var item in tem.Stages)
                    {
                        if (item.PartType == part)
                        {
                            infos.Add(item);
                        }
                    }
                }

                return infos;
            }
            catch (Exception ex)
            {
                return infos;
            }
        }
    }
}

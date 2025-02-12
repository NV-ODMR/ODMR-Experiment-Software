using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controls;
using System.Windows;
using System.Windows.Controls;
using ComboBox = Controls.ComboBox;
using OpenCvSharp.Aruco;

namespace ODMR_Lab.IO操作
{
    /// <summary>
    /// 界面参数基类
    /// </summary>
    public abstract class ConfigBase : ParamBase
    {
        /// <summary>
        /// 生成带参数类型的描述
        /// </summary>
        public Dictionary<string, string> GenerateTaggedDescriptions(string identify = "")
        {
            Dictionary<string, string> dic = GenerateDescription();
            Dictionary<string, string> tar = new Dictionary<string, string>();
            foreach (var item in dic)
            {
                tar.Add(item.Key + "@@" + GetType().Name + "@@" + identify, item.Value);
            }
            return tar;
        }

        /// <summary>
        /// 从带参数类型的描述中读取值
        /// </summary>
        public void ReadFromTaggedDescriptions(Dictionary<string, string> des, string identify)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            foreach (var item in des)
            {
                if (item.Key.Contains("@@" + GetType().Name + "@@" + identify))
                {
                    dic.Add(item.Key.Replace("@@" + GetType().Name + "@@" + identify, ""), item.Value);
                }
            }
            ReadFromDescription(dic);
        }
    }
}

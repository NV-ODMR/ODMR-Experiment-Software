using Controls;
using ODMR_Lab.磁场调节;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ComboBox = Controls.ComboBox;
using Label = System.Windows.Controls.Label;
using TextBox = System.Windows.Controls.TextBox;

namespace ODMR_Lab.IO操作
{
    /// <summary>
    /// 参数基类，提供将参数同步到指定窗口控件中的方法
    /// </summary>
    public abstract class ParamBase
    {
        /// <summary>
        /// 生成文件描述
        /// </summary>
        public Dictionary<string, string> GenerateDescription()
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            PropertyInfo[] infos = GetType().GetProperties();
            foreach (var item in infos)
            {
                var result = item.GetValue(this);
                if (!(result is ParamB)) continue;
                ParamB resp = result as ParamB;
                resp.AddToDesctiption(output);
            }
            return output;
        }

        /// <summary>
        /// 将参数值写入UI中，参数名必须和UI控件名保持一致
        /// </summary>
        /// <param name="eles"></param>
        /// <exception cref="Exception"></exception>
        public void LoadToPage(FrameworkElement[] eles, bool ThrowException = true)
        {
            PropertyInfo[] param = GetType().GetProperties();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in param)
                {
                    if (!typeof(ParamB).IsAssignableFrom(item.PropertyType)) continue;
                    ParamB value = (ParamB)item.GetValue(this);
                    value.LoadToPage(eles, ThrowException);
                }
            });
        }

        /// <summary>
        /// 从UI中读取参数值,如果发生值转换错误则报错
        /// </summary>
        public void ReadFromPage(FrameworkElement[] eles, bool ThrowException = true)
        {
            PropertyInfo[] param = GetType().GetProperties();
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var item in param)
                {
                    if (!typeof(ParamB).IsAssignableFrom(item.PropertyType)) continue;
                    ParamB value = (ParamB)item.GetValue(this);
                    value.ReadFromPage(eles, ThrowException);
                }
            });
        }


        /// <summary>
        /// 生成不含Description的文件描述
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetPureDescription()
        {
            Dictionary<string, string> des = GenerateDescription();
            Dictionary<string, string> newdes = new Dictionary<string, string>();
            foreach (var item in des)
            {
                try
                {
                    newdes.Add(item.Key.Split('→')[1], item.Value);
                }
                catch (Exception) { }
            }
            return newdes;
        }
        /// <summary>
        /// 从userdat文件描述中读取参数
        /// </summary>
        /// <param name="des"></param>
        /// <returns></returns>
        public void ReadFromDescription(Dictionary<string, string> des)
        {
            PropertyInfo[] infos = GetType().GetProperties();
            foreach (var item in infos)
            {
                dynamic result = item.GetValue(this);
                if (!(result is ParamB)) continue;
                ParamB resb = result as ParamB;
                resb.ReadFromDescription(des);
            }
        }


        /// <summary>
        /// 复制备份
        /// </summary>
        /// <returns></returns>
        public object Copy()
        {
            object obj = Activator.CreateInstance(GetType());
            PropertyInfo[] param = GetType().GetProperties();
            foreach (var item in param)
            {
                item.SetValue(obj, item.GetValue(this));
            }
            return obj;
        }

    }
}

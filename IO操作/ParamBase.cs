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
        /// 转换类型(bool,int,double,字符串，枚举)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private dynamic ConvertType(object obj, Type targettype)
        {
            if (typeof(string).IsAssignableFrom(targettype))
            {
                if (obj is Enum)
                {
                    return Enum.GetName(obj.GetType(), obj);
                }
                else
                {
                    return obj.ToString();
                }
            }
            if (typeof(bool).IsAssignableFrom(targettype))
            {
                if (obj is string)
                {
                    return bool.Parse(obj as string);
                }
                if (obj is bool)
                {
                    return obj;
                }
            }
            if (typeof(int).IsAssignableFrom(targettype))
            {
                if (obj is string)
                {
                    return int.Parse(obj as string);
                }
                if (obj is int)
                {
                    return obj;
                }
            }
            if (typeof(double).IsAssignableFrom(targettype))
            {
                if (obj is string)
                {
                    return double.Parse(obj as string);
                }
                if (obj is double)
                {
                    return obj;
                }
            }
            if (typeof(Enum).IsAssignableFrom(targettype))
            {
                if (obj is string)
                {
                    return Enum.Parse(targettype, obj as string);
                }
                if (obj is Enum)
                {
                    return obj;
                }
            }
            throw new Exception("指定类型无法转换");
        }

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
                string name = item.Name + "→" + (result as ParamB).Description;

                ParamB resp = result as ParamB;

                //参数泛型类型
                Type GenericType = result.GetType().GetGenericArguments()[0];

                if (resp.RawValue is double || resp.RawValue is float || resp.RawValue is string || resp.RawValue is bool || resp.RawValue is int)
                {
                    output.Add(name, resp.RawValue.ToString());
                }
                if (resp.RawValue is Enum)
                {
                    output.Add(name, Enum.GetName(resp.RawValue.GetType(), resp.RawValue));
                }
            }
            return output;
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
        /// 将参数值写入UI中，参数名必须和UI控件名保持一致
        /// </summary>
        /// <param name="eles"></param>
        /// <exception cref="Exception"></exception>
        public void LoadToPage(FrameworkElement[] eles)
        {
            PropertyInfo[] param = GetType().GetProperties();
            foreach (var item in param)
            {
                if (!typeof(ParamB).IsAssignableFrom(item.PropertyType)) continue;
                foreach (var ele in eles)
                {
                    var res = ele.FindName(item.Name);
                    if (res == null) continue;
                    ParamB value = (ParamB)item.GetValue(this);
                    if (res is TextBox)
                    {
                        string str = ParamB.GetUnknownParamValue(value).ToString();
                        if (str == "NAN") str = "";
                        (res as TextBox).Text = str;
                        break;
                    }
                    if (res is TextBlock)
                    {
                        string str = ParamB.GetUnknownParamValue(value).ToString();
                        if (str == "NAN") str = "";
                        (res as TextBlock).Text = str;
                        break;
                    }
                    if (res is Label)
                    {
                        string str = ParamB.GetUnknownParamValue(value).ToString();
                        if (str == "NAN") str = "";
                        (res as Label).Content = str;
                        break;
                    }
                    if (res is Chooser)
                    {
                        (res as Chooser).IsSelected = (bool)ParamB.GetUnknownParamValue(value);
                        break;
                    }
                    if (res is ComboBox)
                    {
                        if (typeof(Enum).IsAssignableFrom(value.ValueType))
                        {
                            (res as ComboBox).Select(Enum.GetName(ParamB.GetUnknownParamValue(value).GetType(), ParamB.GetUnknownParamValue(value)));
                        }
                        if (typeof(string).IsAssignableFrom(value.ValueType))
                        {
                            (res as ComboBox).Select(ParamB.GetUnknownParamValue(value));
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 从UI中读取参数值,如果发生值转换错误则报错
        /// </summary>
        public void ReadFromPage(FrameworkElement[] eles, bool ThrowException = true)
        {
            PropertyInfo[] param = GetType().GetProperties();
            foreach (var item in param)
            {
                if (!typeof(ParamB).IsAssignableFrom(item.PropertyType)) continue;
                foreach (var ele in eles)
                {
                    var res = ele.FindName(item.Name);
                    if (res == null) continue;
                    try
                    {
                        if (res is TextBox)
                        {
                            //枚举类型
                            ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as TextBox).Text);
                        }
                        if (res is TextBlock)
                        {
                            //枚举类型
                            ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as TextBlock).Text);
                        }
                        if (res is Label)
                        {
                            ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as Label).Content.ToString());
                        }
                        if (res is Chooser)
                        {
                            ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as Chooser).IsSelected);
                        }
                        if (res is ComboBox)
                        {
                            if ((res as ComboBox).SelectedItem == null)
                            {
                                ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), "");
                            }
                            else
                            {
                                ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as ComboBox).SelectedItem.Text);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (ThrowException)
                            throw new Exception("参数设置错误,试图将" + res.GetType().Name + "的值赋给" + item.PropertyType.Name + ",参数名:" + item.Name);
                        else
                        {
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从UI中读取指定参数
        /// </summary>
        public void ReadSingleFromPageWithException(ParamB TargetParam, FrameworkElement ele)
        {
            PropertyInfo item = GetType().GetProperty(TargetParam.GetType().Name);
            if (!typeof(ParamB).IsAssignableFrom(item.PropertyType))
            {
                throw new Exception("读取参数" + item.Name + "失败");
            };
            var res = ele.FindName(item.Name);
            if (res == null)
            {
                throw new Exception("指定UI中不存在此参数");
            }
            try
            {
                if (res is TextBox)
                {
                    //枚举类型
                    ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as TextBox).Text);
                }
                if (res is Label)
                {
                    ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as Label).Content.ToString());
                }
                if (res is Chooser)
                {
                    ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as Chooser).IsSelected);
                }
                if (res is ComboBox)
                {
                    if ((res as ComboBox).SelectedItem == null)
                    {
                        ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), "");
                    }
                    else
                    {
                        ParamB.SetUnknownParamValue((ParamB)item.GetValue(this), (res as ComboBox).SelectedItem.Text);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("参数设置错误,试图将" + res.GetType().Name + "的值赋给" + item.PropertyType.Name + ",参数名:" + item.Name);
            }
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
                foreach (string keyseg in des.Keys)
                {
                    if (keyseg.Contains(item.Name + "→"))
                    {
                        string description = keyseg.Split('→')[1];

                        if (resb.RawValue is double)
                        {
                            resb.RawValue = double.Parse(des[keyseg]);
                        }
                        if (resb.RawValue is int)
                        {
                            resb.RawValue = int.Parse(des[keyseg]);
                        }
                        if (resb.RawValue is bool)
                        {
                            resb.RawValue = bool.Parse(des[keyseg]);
                        }
                        if (resb.RawValue is float)
                        {
                            resb.RawValue = float.Parse(des[keyseg]);
                        }
                        if (resb.RawValue is string)
                        {
                            resb.RawValue = des[keyseg];
                        }
                        if (resb.RawValue is Enum)
                        {
                            resb.RawValue = Enum.Parse(resb.RawValue.GetType(), des[keyseg]);
                        }
                    }
                }
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

using Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.IO操作
{

    public abstract class ParamB
    {
        public static dynamic GetUnknownParamValue(ParamB obj)
        {
            if (obj.RawValue is double)
            {
                return (double)obj.RawValue;
            }
            if (obj.RawValue is bool)
            {
                return (bool)obj.RawValue;
            }
            if (obj.RawValue is string)
            {
                return (string)obj.RawValue;
            }
            if (obj.RawValue is float)
            {
                return (float)obj.RawValue;
            }
            if (obj.RawValue is int)
            {
                return (int)obj.RawValue;
            }
            if (obj.RawValue is Enum)
            {
                return (Enum)obj.RawValue;
            }
            if (obj.RawValue is DateTime)
            {
                return (DateTime)obj.RawValue;
            }
            return null;
        }

        public static string GetUnknownParamValueToString(ParamB obj)
        {
            if (obj.RawValue is double || obj.RawValue is float || obj.RawValue is string || obj.RawValue is int || obj.RawValue is bool)
            {
                return obj.RawValue.ToString();
            }
            if (obj.RawValue is Enum)
            {
                return Enum.GetName(obj.ValueType, obj.RawValue);
            }
            if (obj.RawValue is DateTime)
            {
                return ((DateTime)obj.RawValue).ToLongTimeString();
            }
            return "";
        }

        public static void SetUnknownParamValue(ParamB obj, object value)
        {
            if (obj.RawValue is double)
            {
                if (value is string)
                {
                    obj.RawValue = double.Parse(value as string);
                }
                else
                {
                    obj.RawValue = (double)value;
                }
            }
            if (obj.RawValue is int)
            {
                if (value is string)
                {
                    obj.RawValue = int.Parse(value as string);
                }
                else
                {
                    obj.RawValue = (int)value;
                }
            }
            if (obj.RawValue is float)
            {
                if (value is string)
                {
                    obj.RawValue = float.Parse(value as string);
                }
                else
                {
                    obj.RawValue = (float)value;
                }
            }
            if (obj.RawValue is bool)
            {
                if (value is string)
                {
                    obj.RawValue = bool.Parse(value as string);
                }
                else
                {
                    obj.RawValue = (bool)value;
                }
            }
            if (obj.RawValue is string)
            {
                if (value is Enum)
                {
                    obj.RawValue = Enum.GetName(value.GetType(), value);
                }
                else
                {
                    obj.RawValue = value.ToString();
                }
            }

            if (obj.RawValue is Enum)
            {
                if (value is string)
                {
                    obj.RawValue = Enum.Parse(obj.ValueType, value as string);
                }
                if (value is Enum)
                {
                    obj.RawValue = value as Enum;
                }
            }
        }

        /// <summary>
        /// 值类型
        /// </summary>
        public Type ValueType { get; protected set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; protected set; } = "";

        /// <summary>
        /// 分组名
        /// </summary>
        public string GroupName { get; set; } = "";

        /// <summary>
        /// 参数值
        /// </summary>
        public object RawValue { get; set; }

        /// <summary>
        /// 属性名
        /// </summary>
        public string PropertyName { get; set; } = "";

        #region 从页面中读取和写入页面
        /// <summary>
        /// 从页面读取值
        /// </summary>
        /// <param name="eles"></param>
        /// <param name="ThrowException"></param>
        /// <returns></returns>
        public object ReadFromPage(FrameworkElement[] eles, bool ThrowException)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var ele in eles)
                {
                    var res = ele.FindName(PropertyName);
                    if (res == null) continue;
                    try
                    {
                        if (res is TextBox)
                        {
                            //枚举类型
                            SetUnknownParamValue(this, (res as TextBox).Text);
                        }
                        if (res is TextBlock)
                        {
                            //枚举类型
                            SetUnknownParamValue(this, (res as TextBlock).Text);
                        }
                        if (res is Label)
                        {
                            SetUnknownParamValue(this, (res as Label).Content.ToString());
                        }
                        if (res is Chooser)
                        {
                            SetUnknownParamValue(this, (res as Chooser).IsSelected);
                        }
                        if (res is ComboBox)
                        {
                            if ((res as ComboBox).SelectedItem == null)
                            {
                                SetUnknownParamValue(this, "");
                            }
                            else
                            {
                                SetUnknownParamValue(this, (res as ComboBox).SelectedItem.Text);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (ThrowException)
                            throw new Exception("参数设置错误,试图将" + res.GetType().Name + "的值赋给" + PropertyName + ",参数名:" + PropertyName);
                        else
                        {
                            continue;
                        }
                    }
                }
            });
            return RawValue;
        }

        /// <summary>
        /// 加载值到页面
        /// </summary>
        /// <param name="eles"></param>
        /// <param name="ThrowException"></param>
        /// <exception cref="Exception"></exception>
        public void LoadToPage(FrameworkElement[] eles, bool ThrowException)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var ele in eles)
                {
                    var res = ele.FindName(PropertyName);
                    if (res == null) continue;
                    ParamB value = this;
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
            });
        }
        #endregion

        #region 从Description中读取和写入
        /// <summary>
        /// 从Description中读取
        /// </summary>
        /// <param name="des"></param>
        public void ReadFromDescription(Dictionary<string, string> des)
        {
            foreach (string keyseg in des.Keys)
            {
                if (keyseg.Contains(PropertyName + "→" + Description))
                {
                    SetUnknownParamValue(this, des[keyseg]);

                    return;
                }
            }
        }

        /// <summary>
        /// 添加到Descrption中
        /// </summary>
        /// <param name="des"></param>
        public void AddToDesctiption(Dictionary<string, string> des)
        {
            string name = PropertyName + "→" + Description;
            if (RawValue is double || RawValue is float || RawValue is string || RawValue is bool || RawValue is int)
            {
                des.Add(name, RawValue.ToString());
            }
            if (RawValue is Enum)
            {
                des.Add(name, Enum.GetName(RawValue.GetType(), RawValue));
            }
        }
        #endregion

        public ParamB Clone()
        {
            return (ParamB)Activator.CreateInstance(GetType(), Description, RawValue, PropertyName);
        }
    }

    /// <summary>
    /// 参数
    /// </summary>
    public class Param<T> : ParamB
    {
        public T Value
        {
            get
            {
                return (T)RawValue;
            }
            set
            {
                RawValue = value;
            }
        }

        public Param(string description, T value, string propertyName)
        {
            Description = description;
            Value = value;
            ValueType = typeof(T);
            PropertyName = propertyName;
        }

        public new T ReadFromPage(FrameworkElement[] eles, bool ThrowException)
        {
            return (T)base.ReadFromPage(eles, ThrowException);
        }

        public new void LoadToPage(FrameworkElement[] eles, bool ThrowException)
        {
            base.LoadToPage(eles, ThrowException);
        }
    }
}

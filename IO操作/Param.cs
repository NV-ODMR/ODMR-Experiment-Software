using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// 参数值
        /// </summary>
        public object RawValue { get; set; }
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

        public Param(string description, T value)
        {
            Description = description;
            Value = value;
            ValueType = typeof(T);
        }
    }
}

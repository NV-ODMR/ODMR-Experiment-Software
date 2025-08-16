using ODMR_Lab.IO操作;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.实验部分.自定义算法
{
    public abstract class AlgorithmBase
    {
        /// <summary>
        /// 输入参数
        /// </summary>
        public abstract List<ParamB> InputParams { get; set; }

        /// <summary>
        /// 输出参数
        /// </summary>
        public abstract List<ParamB> OutputParams { get; set; }

        /// <summary>
        /// 算法名称
        /// </summary>
        public abstract string AlgorithmName { get; }

        /// <summary>
        /// 算法描述
        /// </summary>
        public abstract string AlgorithmDescription { get; }

        /// <summary>
        /// 计算函数
        /// </summary>
        public abstract void CalculateFunc();

        #region 获取输入参数值
        /// <summary>
        /// 根据描述获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public ParamB GetInputParamValueByDescription(string description)
        {
            foreach (var item in InputParams)
            {
                if (item.Description == description)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic GetInputParamValueByName(string name)
        {
            foreach (var item in InputParams)
            {
                if (item.PropertyName == name)
                {
                    return ParamB.GetUnknownParamValue(item);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据参数名获取输入参数
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public dynamic SetInputParamValueByName(string name, object value)
        {
            foreach (var item in InputParams)
            {
                if (item.PropertyName == name)
                {
                    ParamB.SetUnknownParamValue(item, value);
                }
            }
            return null;
        }
        #endregion

    }
}

using CodeHelper;
using Controls;
using ODMR_Lab.自定义实验;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using ComboBox = Controls.ComboBox;

namespace ODMR_Lab.实验部分.自定义实验
{
    /// <summary>
    /// 自定义实验类型
    /// </summary>
    public abstract class CustomExpObject : ExperimentObject<CustomExpParams, CustomConfigParams>
    {

        /// <summary>
        /// 实验名称，显示在按钮中
        /// </summary>
        public abstract string ExperimentName { get; set; }

        /// <summary>
        /// 所属页面
        /// </summary>
        DisplayPage ParentPage = null;

        protected override void InnerRead(FileObject obj)
        {
            foreach (var item in Config.DeviceNameParams)
            {
                item.Value.ReadFromDescription(obj.Descriptions);
            }
            CustomInnerRead(obj);
        }

        protected override FileObject InnerWrite()
        {
            FileObject obj = new FileObject();
            foreach (var item in Config.DeviceNameParams)
            {
                item.Value.AddToDesctiption(obj.Descriptions);
            }
            CustomInnerWrite(obj);
            return obj;
        }

        /// <summary>
        /// 读文件
        /// </summary>
        /// <param name="obj"></param>
        protected abstract void CustomInnerRead(FileObject obj);

        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="obj"></param>
        protected abstract void CustomInnerWrite(FileObject obj);
    }
}

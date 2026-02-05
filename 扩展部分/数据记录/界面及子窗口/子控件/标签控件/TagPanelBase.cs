using ODMR_Lab.扩展部分.数据记录.界面及子窗口;
using ODMR_Lab.数据记录;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ODMR_Lab.扩展部分.数据记录
{
    public abstract class TagPanelBase : Grid
    {
        /// <summary>
        /// 设置状态：可以改变选项，无法修改选项内容(部分修改)
        /// </summary>
        public abstract void SetPartialEditable();

        /// <summary>
        /// 设置状态：可以改变所有内容(完全修改)
        /// </summary>
        public abstract void SetFullyEditable();

        /// <summary>
        /// 设置状态：不可改变所有内容(完全禁止编辑)
        /// </summary>
        public abstract void SetFullyUnEditable();

        /// <summary>
        /// 设置锁定状态
        /// </summary>
        public abstract void SetLockDisplay();

        /// <summary>
        /// 设置未锁定状态
        /// </summary>
        public abstract void SetUnLockDisplay();

        public abstract event RoutedEventHandler ColorSelectClicked;

        public abstract void SetSelectorColor(Color color);

        public abstract Color GetSelectorColor();
    }
}

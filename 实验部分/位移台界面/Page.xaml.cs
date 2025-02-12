using CodeHelper;
using Controls;
using HardWares.温度控制器.SRS_PTC10;
using HardWares.纳米位移台.PI;
using ODMR_Lab.Windows;
using ODMR_Lab.位移台部分;
using ODMR_Lab.实验部分.磁场调节;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Xml.Linq;
using Path = System.IO.Path;
using MathNet.Numerics.Distributions;
using System.Diagnostics.Contracts;
using MathNet.Numerics;
using ODMR_Lab.Python.LbviewHandler;
using System.Windows.Forms;
using ContextMenu = Controls.ContextMenu;
using ODMR_Lab.实验部分.样品定位;
using Label = System.Windows.Controls.Label;
using ODMR_Lab.基本控件;
using Clipboard = System.Windows.Clipboard;
using Controls.Windows;
using Window = System.Windows.Window;

namespace ODMR_Lab.位移台界面
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public DisplayPage()
        {
            InitializeComponent();

        }
        public override void Init()
        {
        }


        public override void CloseBehaviour()
        {
        }

        public override void UpdateParam()
        {
        }
    }
}

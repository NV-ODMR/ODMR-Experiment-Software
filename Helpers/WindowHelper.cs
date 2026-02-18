using Controls.Windows;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab
{
    public class WindowHelper
    {
        public static void SetContent(MessageWindow window, string content)
        {
            if (window == null) { return; }
            App.Current.Dispatcher.Invoke(() =>
            {
                window.SetContent(content);
            });
        }
    }
}

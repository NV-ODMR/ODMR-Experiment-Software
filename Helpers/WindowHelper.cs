using Controls.Windows;

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

using ODMR_Lab.设备部分.位移台部分;
using System;
using System.Threading;
using System.Windows;

namespace ODMR_Lab.位移台界面
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class DisplayPage : ExpPageBase
    {
        public override string PageName { get; set; } = "位移台控制台";

        public DisplayPage()
        {
            InitializeComponent();
        }

        public override void InnerInit()
        {
            ProbePanel.MoverPart = PartTypes.Probe;
            MWPanel.MoverPart = PartTypes.Microwave;
            SamplePanel.MoverPart = PartTypes.Sample;
            MagnetPanel.MoverPart = PartTypes.Magnnet;
            LenPanel.MoverPart = PartTypes.Len;
            CreateListener();
        }

        Thread Listener = null;

        /// <summary>
        /// 位移台位置刷新间隔（毫秒）。
        /// 轮询治理（2026-10-05）：原为 50ms，5 个面板 × 最多 6 个轴 ⇒ 最坏 30 次位置读 / 50ms ≈ 600 次/秒，
        /// 是全程序最大的设备轮询源；现放宽到 200ms，且页面不可见时完全不读设备
        /// （门控在 StageControlPanel.UpdateListenerState，判据 V6）。
        /// </summary>
        public const int ListenerGapMs = 200;

        /// <summary>
        /// 线程退出标志（替代 Thread.Abort：避免中断正卡在 Dispatcher/设备调用中的线程）
        /// </summary>
        private volatile bool IsListenerEnd = false;

        public void CreateListener()
        {
            if (Listener != null) return;
            IsListenerEnd = false;
            Listener = new Thread(() =>
            {
                while (!IsListenerEnd)
                {
                    ProbePanel.UpdateListenerState();
                    SamplePanel.UpdateListenerState();
                    MagnetPanel.UpdateListenerState();
                    MWPanel.UpdateListenerState();
                    LenPanel.UpdateListenerState();
                    Thread.Sleep(ListenerGapMs);
                }
            });
            Listener.IsBackground = true;
            Listener.Start();
        }


        public override void CloseBehaviour()
        {
            IsListenerEnd = true;
            try
            {
                Listener?.Join(2000);
            }
            catch (Exception)
            {
            }
        }

        public override void UpdateParam()
        {
        }
    }
}

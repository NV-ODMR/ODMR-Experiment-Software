using ODMR_Lab.设备部分.位移台部分;
using System.Threading;

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

        public void CreateListener()
        {
            Listener = new Thread(() =>
            {
                while (true)
                {
                    ProbePanel.UpdateListenerState();
                    SamplePanel.UpdateListenerState();
                    MagnetPanel.UpdateListenerState();
                    MWPanel.UpdateListenerState();
                    LenPanel.UpdateListenerState();
                    Thread.Sleep(50);
                }
            });
            Listener.Start();
        }


        public override void CloseBehaviour()
        {
            Listener?.Abort();
            while (Listener.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(20);
            }
        }

        public override void UpdateParam()
        {
        }
    }
}

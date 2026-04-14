using CodeHelper;
using HardWares;
using HardWares.相机_CCD_;
using ODMR_Lab.设备部分;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.设备部分.相机_翻转镜
{
    public class CameraInfo : DeviceInfoBase<CameraBase>
    {
        public override bool IsLoadParams { get; set; } = true;
        public CameraWindow DisplayWindow { get; set; } = null;

        public override void CreateDeviceInfoBehaviour()
        {
            CloseDeviceEvent += new Action(() =>
            {
                if (DisplayWindow != null && DisplayWindow.IsActive)
                {
                    DisplayWindow.CancelThread();
                    DisplayWindow.Close();
                    DisplayWindow = null;
                }
            });
        }

        public override string GetDeviceDescription()
        {
            return Device.ProductName;
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }
    }
}

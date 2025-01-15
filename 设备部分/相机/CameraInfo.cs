using CodeHelper;
using HardWares;
using HardWares.相机_CCD_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab.相机
{
    public class CameraInfo : DeviceInfoBase<CameraBase>
    {

        public CameraWindow DisplayWindow { get; set; } = null;

        public override DeviceInfoBase<CameraBase> CreateDeviceInfo(CameraBase device, DeviceConnectInfo connectinfo)
        {
            CameraInfo info = new CameraInfo() { ConnectInfo = connectinfo, Device = device };
            info.CloseDeviceEvent += new Action(() =>
            {
                if (info.DisplayWindow != null && info.DisplayWindow.IsActive)
                {
                    info.DisplayWindow.CancelThread();
                    info.DisplayWindow.Close();
                    info.DisplayWindow = null;
                }
            });
            return info;
        }

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }
    }
}

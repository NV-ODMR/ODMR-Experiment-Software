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

        protected override void AutoConnectedAction(FileObject file)
        {
        }

        protected override void CloseFileAction(FileObject obj)
        {
        }
    }
}

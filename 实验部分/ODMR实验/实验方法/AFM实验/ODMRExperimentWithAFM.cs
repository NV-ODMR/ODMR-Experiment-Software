using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controls.Windows;
using System.Windows;
using ODMR_Lab.ODMR实验;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using ODMR_Lab.IO操作;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.AFM
{
    public abstract class ODMRExperimentWithAFM : ODMRExpObject
    {
        public override void ODMRExperiment()
        {
            ODMRExpWithAFM();
        }

        public abstract void ODMRExpWithAFM();

        public override void AfterExpEvent()
        {
            AfterExpEventWithAFM();
            SetExpState("正在撤针...");
            //撤针
            AFMStopDrop drop = new AFMStopDrop();
            drop.CoreMethod(new List<object>(), GetLockIn());
            SetExpState("");
        }

        /// <summary>
        /// 实验结束后,AFM撤针前,要进行的操作
        /// </summary>
        public abstract void AfterExpEventWithAFM();

        public override void PreExpEvent()
        {
            LockinInfo info = GetLockIn();
            //下针信息确认
            bool iscontinue = true;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (MessageWindow.ShowMessageBox("下针信息确认", "当前振幅:" + info.Device.DemodR.ToString() + "\n" + "设定点:" + info.Device.SetPoint.ToString() + "\n"
                    + "P:" + info.Device.P.ToString() + "\n" + "I:" + info.Device.I.ToString() + "\n" + "D:" + info.Device.D.ToString() + "\n" + "是否继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) != MessageBoxResult.Yes)
                {
                    iscontinue = false;
                }
            });
            if (!iscontinue) throw new Exception("实验已停止");

            //添加下针参数到输出参数栏
            OutputParams.Add(new Param<double>("下针参数P", info.Device.P, "DropP"));
            OutputParams.Add(new Param<double>("下针参数I", info.Device.I, "DropI"));
            OutputParams.Add(new Param<double>("下针参数D", info.Device.D, "DropD"));
            OutputParams.Add(new Param<double>("下针参数Signal", info.Device.DemodR, "DropSignal"));
            OutputParams.Add(new Param<double>("下针参数SetPoint", info.Device.SetPoint, "DropSetPoint"));

            PreExpEventBeforeDropWithAFM();
            if (GetAllowMultiDrop())
            {
                //多次下针
                NanoStageInfo samplez = GetSampleZ();
                bool movereverse = GetSampleAxisReverse();
                int maxDropLoop = GetMaxDropCount();
                double dropDet = GetDropDet();
                bool dropsucceed = false;
                for (int i = 0; i < maxDropLoop; i++)
                {
                    SetExpState("正在下针...当前尝试次数:" + (i + 1).ToString());
                    //下针
                    AFMDrop drop = new AFMDrop();
                    var result = drop.CoreMethod(new List<object>(), GetLockIn());
                    if ((bool)result[0] == true)
                    {
                        dropsucceed = true;
                        break;
                    }
                    JudgeThreadEndOrResumeAction?.Invoke();
                    SetExpState("当前下针尝试失败，正在撤针...");
                    //撤针，升高样品
                    AFMStopDrop stdrop = new AFMStopDrop();
                    stdrop.CoreMethod(new List<object>(), GetLockIn());
                    JudgeThreadEndOrResumeAction?.Invoke();
                    //升高位移台
                    samplez.Device.MoveStepAndWait(dropDet * (movereverse ? -1 : 1), 3000);
                }
                if (!dropsucceed)
                {
                    ///没下到针
                    throw new Exception("下针未完成,达到最大尝试次数");
                }
            }
            else
            {
                SetExpState("正在下针...");
                //下针
                AFMDrop drop = new AFMDrop();
                var result = drop.CoreMethod(new List<object>(), GetLockIn());
                if ((bool)result[0] == false)
                {
                    ///没下到针
                    throw new Exception("下针未完成,PID输出已经到达最大值");
                }
            }
            PreExpEventWithAFM();
        }

        protected abstract bool GetAllowMultiDrop();

        protected abstract int GetMaxDropCount();

        protected abstract double GetDropDet();

        protected abstract bool GetSampleAxisReverse();

        protected abstract NanoStageInfo GetSampleZ();

        /// <summary>
        /// AFM下针后,实验开始前要进行的操作
        /// </summary>
        public abstract void PreExpEventWithAFM();

        /// <summary>
        /// AFM下针前,实验开始前要进行的操作
        /// </summary>
        public abstract void PreExpEventBeforeDropWithAFM();

        /// <summary>
        /// 获取AFM所需的LockIn设备
        /// </summary>
        /// <returns></returns>
        public abstract LockinInfo GetLockIn();
    }
}

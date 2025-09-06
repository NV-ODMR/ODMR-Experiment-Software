using Controls.Windows;
using ODMR_Lab.IO操作;
using ODMR_Lab.实验部分.ODMR实验.实验方法.ScanCore;
using ODMR_Lab.实验部分.ODMR实验.实验方法.无AFM实验.单点.脉冲实验;
using ODMR_Lab.设备部分.位移台部分;
using ODMR_Lab.设备部分.射频源_锁相放大器;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.实验部分.ODMR实验.实验方法.梯度测量相关实验
{
    public abstract class VibrationExpBase : PulseExpBase
    {
        public override bool IsAFMSubExperiment { get; protected set; } = false;
        public override string ODMRExperimentGroupName { get; set; } = "梯度测量相关实验";

        protected void MultiDropProcedure(bool IsMultiDrop, bool movereverse, int maxDropLoop, double dropDet, NanoStageInfo samplez, LockinInfo lockin)
        {
            #region 下针
            if (IsMultiDrop)
            {
                //多次下针
                bool dropsucceed = false;
                for (int i = 0; i < maxDropLoop; i++)
                {
                    SetExpState("正在下针...当前尝试次数:" + (i + 1).ToString());
                    //下针
                    AFMDrop drop = new AFMDrop();
                    var result = drop.CoreMethod(new List<object>(), lockin);
                    if ((bool)result[0] == true)
                    {
                        dropsucceed = true;
                        break;
                    }
                    JudgeThreadEndOrResumeAction?.Invoke();
                    SetExpState("当前下针尝试失败，正在撤针...");
                    //撤针，升高样品
                    AFMStopDrop stdrop = new AFMStopDrop();
                    stdrop.CoreMethod(new List<object>(), lockin);
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
                var result = drop.CoreMethod(new List<object>(), lockin);
                if ((bool)result[0] == false)
                {
                    ///没下到针
                    throw new Exception("下针未完成,PID输出已经到达最大值");
                }
            }
            #endregion
        }

        protected bool DropConfirm(LockinInfo lockin)
        {
            //下针信息确认
            bool iscontinue = true;
            App.Current.Dispatcher.Invoke(() =>
            {
                if (MessageWindow.ShowMessageBox("下针信息确认", "当前振幅:" + lockin.Device.DemodR.ToString() + "\n" + "设定点:" + lockin.Device.SetPoint.ToString() + "\n"
                    + "P:" + lockin.Device.P.ToString() + "\n" + "I:" + lockin.Device.I.ToString() + "\n" + "D:" + lockin.Device.D.ToString() + "\n" + "是否继续?", MessageBoxButton.YesNo, owner: Window.GetWindow(ParentPage)) != MessageBoxResult.Yes)
                {
                    iscontinue = false;
                }
            });
            if (!iscontinue) return false;
            //添加下针参数到输出参数栏
            OutputParams.Add(new Param<double>("下针参数P", lockin.Device.P, "DropP"));
            OutputParams.Add(new Param<double>("下针参数I", lockin.Device.I, "DropI"));
            OutputParams.Add(new Param<double>("下针参数D", lockin.Device.D, "DropD"));
            OutputParams.Add(new Param<double>("下针参数Signal", lockin.Device.DemodR, "DropSignal"));
            OutputParams.Add(new Param<double>("下针参数SetPoint", lockin.Device.SetPoint, "DropSetPoint"));
            return true;
        }
    }
}

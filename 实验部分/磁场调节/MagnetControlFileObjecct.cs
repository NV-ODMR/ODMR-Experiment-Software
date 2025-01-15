using CodeHelper;
using Controls;
using ODMR_Lab.IO操作;
using ODMR_Lab.Windows;
using ODMR_Lab.数据处理;
using ODMR_Lab.磁场调节;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using ComboBox = Controls.ComboBox;
using DisplayPage = ODMR_Lab.磁场调节.DisplayPage;

namespace ODMR_Lab.实验部分.磁场调节
{
    /// <summary>
    /// 磁场调节的文件类
    /// </summary>
    public class MagnetControlFileObjecct : ExperimentFileObject<MagnetScanParams>
    {
        /// <summary>
        /// X扫描点
        /// </summary>
        public List<CWPointObject> XPoints { get; set; } = new List<CWPointObject>();

        /// <summary>
        /// Y扫描点
        /// </summary>
        public List<CWPointObject> YPoints { get; set; } = new List<CWPointObject>();

        /// <summary>
        /// Z扫描点
        /// </summary>
        public List<CWPointObject> ZPoints { get; set; } = new List<CWPointObject>();

        /// <summary>
        /// 角度扫描点
        /// </summary>
        public List<CWPointObject> AnglePoints { get; set; } = new List<CWPointObject>();

        /// <summary>
        /// 检查点
        /// </summary>
        public List<CWPointObject> CheckPoints { get; set; } = new List<CWPointObject>();


        /// <summary>
        /// 参数列表
        /// </summary>
        public override MagnetScanParams Param { get; set; } = new MagnetScanParams();


        public override ExperimentFileTypes ExpType { get; protected set; } = ExperimentFileTypes.磁场调节;

        /// <summary>
        /// 保存到文件
        /// </summary>
        public void SaveToFile(string FolderPath)
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }
        }

        protected override void InnerRead(FileObject fobj)
        {
            #region X方向结果
            List<double> xpos = fobj.ExtractDouble("Xpos");
            List<double> xcw1s = fobj.ExtractDouble("Xcw1s");
            List<double> xcw2s = fobj.ExtractDouble("Xcw2s");
            if (xpos.Count == xcw1s.Count && xpos.Count == xcw2s.Count)
            {
                //读取X数据
                for (int i = 0; i < xpos.Count; i++)
                {
                    List<double> freqs1 = fobj.ExtractDouble("XFreq1_" + i.ToString());
                    List<double> values1 = fobj.ExtractDouble("XContract1_" + i.ToString());
                    List<double> freqs2 = fobj.ExtractDouble("XFreq2_" + i.ToString());
                    List<double> values2 = fobj.ExtractDouble("XContract2_" + i.ToString());
                    XPoints.Add(new CWPointObject(xpos[i], xcw1s[i], xcw2s[i], 2870, freqs1, values1, freqs2, values2));
                }
            }
            #endregion

            #region Y方向结果
            List<double> ypos = fobj.ExtractDouble("Ypos");
            List<double> ycw1s = fobj.ExtractDouble("Ycw1s");
            List<double> ycw2s = fobj.ExtractDouble("Ycw2s");
            if (ypos.Count == ycw1s.Count && ypos.Count == ycw2s.Count)
            {
                //读取X数据
                for (int i = 0; i < ypos.Count; i++)
                {
                    List<double> freqs1 = fobj.ExtractDouble("YFreq1_" + i.ToString());
                    List<double> values1 = fobj.ExtractDouble("YContract1_" + i.ToString());
                    List<double> freqs2 = fobj.ExtractDouble("YFreq2_" + i.ToString());
                    List<double> values2 = fobj.ExtractDouble("YContract2_" + i.ToString());
                    YPoints.Add(new CWPointObject(ypos[i], ycw1s[i], ycw2s[i], 2870, freqs1, values1, freqs2, values2));
                }
            }
            #endregion

            #region 角度方向结果
            List<double> apos = fobj.ExtractDouble("Apos");
            List<double> acw1s = fobj.ExtractDouble("Acw1s");
            List<double> acw2s = fobj.ExtractDouble("Acw2s");
            if (apos.Count == acw1s.Count && apos.Count == acw2s.Count)
            {
                //读取X数据
                for (int i = 0; i < apos.Count; i++)
                {
                    List<double> freqs1 = fobj.ExtractDouble("AFreq1_" + i.ToString());
                    List<double> values1 = fobj.ExtractDouble("AContract1_" + i.ToString());
                    List<double> freqs2 = fobj.ExtractDouble("AFreq2_" + i.ToString());
                    List<double> values2 = fobj.ExtractDouble("AContract2_" + i.ToString());
                    AnglePoints.Add(new CWPointObject(apos[i], acw1s[i], acw2s[i], 2870, freqs1, values1, freqs2, values2));
                }
            }
            #endregion

            #region Z方向结果
            List<double> zpos = fobj.ExtractDouble("Zpos");
            List<double> zcw1s = fobj.ExtractDouble("Zcw1s");
            List<double> zcw2s = fobj.ExtractDouble("Zcw2s");
            if (zpos.Count == zcw1s.Count && zpos.Count == zcw2s.Count)
            {
                //读取X数据
                for (int i = 0; i < zpos.Count; i++)
                {
                    List<double> freqs1 = fobj.ExtractDouble("ZFreq1_" + i.ToString());
                    List<double> values1 = fobj.ExtractDouble("ZContract1_" + i.ToString());
                    List<double> freqs2 = fobj.ExtractDouble("ZFreq2_" + i.ToString());
                    List<double> values2 = fobj.ExtractDouble("ZContract2_" + i.ToString());
                    ZPoints.Add(new CWPointObject(zpos[i], zcw1s[i], zcw2s[i], 2870, freqs1, values1, freqs2, values2));
                }
            }
            #endregion

            #region 检验结果
            List<double> cpos = fobj.ExtractDouble("Cpos");
            List<double> ccw1s = fobj.ExtractDouble("Ccw1s");
            List<double> ccw2s = fobj.ExtractDouble("Ccw2s");
            if (cpos.Count == ccw1s.Count && cpos.Count == ccw2s.Count)
            {
                //读取X数据
                for (int i = 0; i < cpos.Count; i++)
                {
                    List<double> freqs1 = fobj.ExtractDouble("CFreq1_" + i.ToString());
                    List<double> values1 = fobj.ExtractDouble("CContract1_" + i.ToString());
                    List<double> freqs2 = fobj.ExtractDouble("CFreq2_" + i.ToString());
                    List<double> values2 = fobj.ExtractDouble("CContract2_" + i.ToString());
                    ZPoints.Add(new CWPointObject(cpos[i], ccw1s[i], ccw2s[i], 2870, freqs1, values1, freqs2, values2));
                }
            }
            #endregion
        }

        protected override FileObject InnerWrite()
        {
            FileObject obj = new FileObject();

            #region X方向结果
            List<double> xpos = new List<double>();
            List<double> xcw1s = new List<double>();
            List<double> xcw2s = new List<double>();
            List<List<double>> xfreqs1 = new List<List<double>>();
            List<List<double>> xcontracts1 = new List<List<double>>();
            List<List<double>> xfreqs2 = new List<List<double>>();
            List<List<double>> xcontracts2 = new List<List<double>>();
            foreach (var item in XPoints)
            {
                xpos.Add(item.MoverLoc);
                xcw1s.Add(item.CW1);
                xcw2s.Add(item.CW2);
                xfreqs1.Add(item.CW1Freqs);
                xcontracts1.Add(item.CW1Values);
                xfreqs2.Add(item.CW2Freqs);
                xcontracts2.Add(item.CW2Values);
            }
            obj.WriteDoubleData("Xpos", xpos);
            obj.WriteDoubleData("Xcw1s", xcw1s);
            obj.WriteDoubleData("Xcw2s", xcw2s);
            for (int i = 0; i < xfreqs1.Count; i++)
            {
                obj.WriteDoubleData("XFreq1_" + i.ToString(), xfreqs1[i]);
                obj.WriteDoubleData("XContract1_" + i.ToString(), xcontracts1[i]);
            }
            for (int i = 0; i < xfreqs2.Count; i++)
            {
                obj.WriteDoubleData("XFreq2_" + i.ToString(), xfreqs2[i]);
                obj.WriteDoubleData("XContract2_" + i.ToString(), xcontracts2[i]);
            }
            #endregion

            #region Y方向结果
            List<double> ypos = new List<double>();
            List<double> ycw1s = new List<double>();
            List<double> ycw2s = new List<double>();
            List<List<double>> yfreqs1 = new List<List<double>>();
            List<List<double>> ycontracts1 = new List<List<double>>();
            List<List<double>> yfreqs2 = new List<List<double>>();
            List<List<double>> ycontracts2 = new List<List<double>>();
            foreach (var item in YPoints)
            {
                ypos.Add(item.MoverLoc);
                ycw1s.Add(item.CW1);
                ycw2s.Add(item.CW2);
                yfreqs1.Add(item.CW1Freqs);
                ycontracts1.Add(item.CW1Values);
                yfreqs2.Add(item.CW2Freqs);
                ycontracts2.Add(item.CW2Values);
            }
            obj.WriteDoubleData("Ypos", ypos);
            obj.WriteDoubleData("Ycw1s", ycw1s);
            obj.WriteDoubleData("Ycw2s", ycw2s);
            for (int i = 0; i < yfreqs1.Count; i++)
            {
                obj.WriteDoubleData("YFreq1_" + i.ToString(), yfreqs1[i]);
                obj.WriteDoubleData("YContract1_" + i.ToString(), ycontracts1[i]);
            }
            for (int i = 0; i < yfreqs2.Count; i++)
            {
                obj.WriteDoubleData("YFreq2_" + i.ToString(), yfreqs2[i]);
                obj.WriteDoubleData("YContract2_" + i.ToString(), ycontracts2[i]);
            }
            #endregion

            #region Z方向结果
            List<double> zpos = new List<double>();
            List<double> zcw1s = new List<double>();
            List<double> zcw2s = new List<double>();
            List<List<double>> zfreqs1 = new List<List<double>>();
            List<List<double>> zcontracts1 = new List<List<double>>();
            List<List<double>> zfreqs2 = new List<List<double>>();
            List<List<double>> zcontracts2 = new List<List<double>>();
            foreach (var item in ZPoints)
            {
                zpos.Add(item.MoverLoc);
                zcw1s.Add(item.CW1);
                zcw2s.Add(item.CW2);
                zfreqs1.Add(item.CW1Freqs);
                zcontracts1.Add(item.CW1Values);
                zfreqs2.Add(item.CW2Freqs);
                zcontracts2.Add(item.CW2Values);
            }
            obj.WriteDoubleData("Zpos", zpos);
            obj.WriteDoubleData("Zcw1s", zcw1s);
            obj.WriteDoubleData("Zcw2s", zcw2s);
            for (int i = 0; i < zfreqs1.Count; i++)
            {
                obj.WriteDoubleData("ZFreq1_" + i.ToString(), zfreqs1[i]);
                obj.WriteDoubleData("ZContract1_" + i.ToString(), zcontracts1[i]);
            }
            for (int i = 0; i < zfreqs2.Count; i++)
            {
                obj.WriteDoubleData("ZFreq2_" + i.ToString(), zfreqs2[i]);
                obj.WriteDoubleData("ZContract2_" + i.ToString(), zcontracts2[i]);
            }
            #endregion

            #region 角度结果
            List<double> apos = new List<double>();
            List<double> acw1s = new List<double>();
            List<double> acw2s = new List<double>();
            List<List<double>> afreqs1 = new List<List<double>>();
            List<List<double>> acontracts1 = new List<List<double>>();
            List<List<double>> afreqs2 = new List<List<double>>();
            List<List<double>> acontracts2 = new List<List<double>>();
            foreach (var item in AnglePoints)
            {
                apos.Add(item.MoverLoc);
                acw1s.Add(item.CW1);
                acw2s.Add(item.CW2);
                afreqs1.Add(item.CW1Freqs);
                acontracts1.Add(item.CW1Values);
                afreqs2.Add(item.CW2Freqs);
                acontracts2.Add(item.CW2Values);
            }
            obj.WriteDoubleData("Apos", apos);
            obj.WriteDoubleData("Acw1s", acw1s);
            obj.WriteDoubleData("Acw2s", acw2s);
            for (int i = 0; i < afreqs1.Count; i++)
            {
                obj.WriteDoubleData("AFreq1_" + i.ToString(), afreqs1[i]);
                obj.WriteDoubleData("AContract1_" + i.ToString(), acontracts1[i]);
            }
            for (int i = 0; i < afreqs2.Count; i++)
            {
                obj.WriteDoubleData("AFreq2_" + i.ToString(), afreqs2[i]);
                obj.WriteDoubleData("AContract2_" + i.ToString(), acontracts2[i]);
            }
            #endregion

            #region 检验结果
            List<double> cpos = new List<double>();
            List<double> ccw1s = new List<double>();
            List<double> ccw2s = new List<double>();
            List<List<double>> cfreqs1 = new List<List<double>>();
            List<List<double>> ccontracts1 = new List<List<double>>();
            List<List<double>> cfreqs2 = new List<List<double>>();
            List<List<double>> ccontracts2 = new List<List<double>>();
            foreach (var item in CheckPoints)
            {
                cpos.Add(item.MoverLoc);
                ccw1s.Add(item.CW1);
                ccw2s.Add(item.CW2);
                cfreqs1.Add(item.CW1Freqs);
                ccontracts1.Add(item.CW1Values);
                cfreqs2.Add(item.CW2Freqs);
                ccontracts2.Add(item.CW2Values);
            }
            obj.WriteDoubleData("Cpos", cpos);
            obj.WriteDoubleData("Ccw1s", ccw1s);
            obj.WriteDoubleData("Ccw2s", ccw2s);
            for (int i = 0; i < cfreqs1.Count; i++)
            {
                obj.WriteDoubleData("CFreq1_" + i.ToString(), cfreqs1[i]);
                obj.WriteDoubleData("CContract1_" + i.ToString(), ccontracts1[i]);
            }
            for (int i = 0; i < cfreqs2.Count; i++)
            {
                obj.WriteDoubleData("CFreq2_" + i.ToString(), cfreqs2[i]);
                obj.WriteDoubleData("CContract2_" + i.ToString(), ccontracts2[i]);
            }
            #endregion

            return obj;
        }

        public void LoadToWindow(DisplayPage page)
        {
            try
            {
                #region 加载参数
                Param.LoadToPage(new System.Windows.FrameworkElement[] { page });
                #endregion
                #region 加载X窗口
                page.XWin.AddDatas(XPoints);
                #endregion
                #region 加载Y窗口
                page.YWin.AddDatas(YPoints);
                #endregion
                #region 加载Z窗口
                page.ZWin.SetPoint1(ZPoints[0]);
                page.ZWin.SetPoint2(ZPoints[1]);
                #endregion
                #region 加载角度窗口
                page.AngleWin.AddDatas(AnglePoints);
                #endregion
                #region 加载检查窗口
                page.CheckWin.SetPoint1(CheckPoints[0]);
                page.CheckWin.SetPoint2(CheckPoints[1]);
                #endregion
            }
            catch (Exception e)
            {
                MessageWindow.ShowTipWindow("加载文件出现错误:\n" + e.Message, MainWindow.Handle);
            }
        }

        public override DataVisualSource ToDataVisualSource()
        {
            return new DataVisualSource();
        }
    }
}

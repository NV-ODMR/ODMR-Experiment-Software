using CodeHelper;
using Controls.Windows;
using MathLib.NormalMath.Decimal;
using MathLib.NormalMath.Decimal.Function;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab.基本窗口.数据拟合
{
    public class FitFunc
    {
        static FitFunc()
        {
            UpdateFitFuncs();
        }

        public static List<FitFunc> FitFuncs { get; set; } = new List<FitFunc>();

        public string Name { get; private set; } = "";

        public List<FitParam> TypicalParams { get; private set; } = new List<FitParam>();

        public NormalRealFunction Function { get; set; } = null;

        public string Var { get; private set; } = "";

        public int ParamCount { get; private set; } = 0;

        /// <summary>
        /// 函数描述
        /// </summary>
        public string Description { get; private set; } = "";

        /// <summary>
        /// 分组名称
        /// </summary>
        public string GroupName { get; private set; } = "";

        public FitFunc(string expression, string funcname, string varname, List<string> paramnames, List<double> typicalvalues, List<double> typicalranges)
        {
            Function = new NormalRealFunction(expression);
            Name = funcname;
            Var = varname;
            for (int i = 0; i < typicalvalues.Count; i++)
            {
                TypicalParams.Add(new FitParam() { Name = paramnames[i], Range = typicalranges[i], InitValue = typicalvalues[i] });
            }

            Function.AddVariable(varname, new RealDomain(0, 1, 1));
            foreach (var item in paramnames)
            {
                Function.AddVariable(item, new RealDomain(0, 1, 1));
            }
            Function.Process();
        }

        public FitFunc(string expression, string funcname, List<FitParam> typicalParams, string var, string description, string groupName)
        {
            Function = new NormalRealFunction(expression);

            Name = funcname;

            Function.AddVariable(var, new RealDomain(0, 1, 1));
            List<string> pnames = typicalParams.Select(x => x.Name).ToList();
            foreach (var item in pnames)
            {
                Function.AddVariable(item, new RealDomain(0, 1, 1));
            }
            Function.Process();

            TypicalParams = typicalParams;
            Var = var;
            Description = description;
            GroupName = groupName;
        }

        public double Calculate(double varvalue)
        {
            List<double> vs = new List<double>();
            vs.Add(varvalue);
            vs.AddRange(TypicalParams.Select(x => x.InitValue));
            return Function.GetValueAt(vs).Content.Last();
        }

        /// <summary>
        /// 拟合数据
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="valueguess"></param>
        /// <param name="valuerange"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public Dictionary<string, double> FitData(List<double> x, List<double> y, List<double> valueguess, List<double> valuerange, AlgorithmType algorithm)
        {
            CurveFitting curve = new CurveFitting(Function.Expression, Var, TypicalParams.Select(v => v.Name).ToList());
            return curve.FitCurve(x, y, valueguess, valuerange, algorithm);
        }

        /// <summary>
        /// 刷新拟合函数
        /// </summary>
        public static void UpdateFitFuncs()
        {
            FitFuncs.Clear();
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "FitCurves")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "FitCurves"));
            }
            DirectoryInfo info = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "FitCurves"));
            var files = info.EnumerateFiles();
            foreach (var item in files)
            {
                FitFunc func = ReadFromFile(item.FullName);
                if (func != null)
                {
                    FitFuncs.Add(func);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void WriteToFile()
        {
            try
            {
                FileObject obj = new FileObject();
                obj.Descriptions.Add("Expression", Function.Expression);
                obj.Descriptions.Add("Description", Description);
                obj.Descriptions.Add("GroupName", GroupName);
                obj.Descriptions.Add("Name", Name);
                obj.WriteStringData("Variable", new List<string>() { Var });
                obj.WriteDoubleData("ParamsRange", TypicalParams.Select(x => x.Range).ToList());
                obj.WriteDoubleData("ParamsValue", TypicalParams.Select(x => x.InitValue).ToList());
                obj.WriteStringData("ParamsName", TypicalParams.Select(x => x.Name).ToList());
                obj.SaveToFile(Path.Combine(Environment.CurrentDirectory, "FitCurves", Description + "@@" + Function.Expression));
            }
            catch (Exception e)
            {
                MessageWindow.ShowTipWindow("拟合曲线未成功保存:\n" + e.Message, MainWindow.Handle);
            }
        }

        public void DeleteFromFile()
        {
            File.Delete(Path.Combine(Environment.CurrentDirectory, "FitCurves", Description + "@@" + Function.Expression));
        }

        /// <summary>
        /// 
        /// </summary>
        private static FitFunc ReadFromFile(string FilePath)
        {
            try
            {
                FileObject obj = FileObject.ReadFromFile(FilePath);
                return new FitFunc(obj.Descriptions["Expression"], obj.Descriptions["Name"], obj.ExtractString("Variable")[0], obj.ExtractString("ParamsName"), obj.ExtractDouble("ParamsValue"), obj.ExtractDouble("ParamsRange"))
                {
                    Description = obj.Descriptions["Description"],
                    GroupName = obj.Descriptions["GroupName"]
                };

            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    public class FitParam
    {
        public string Name { get; set; } = "";

        public double Range { get; set; } = 0;

        public double InitValue { get; set; } = 0;
    }
}

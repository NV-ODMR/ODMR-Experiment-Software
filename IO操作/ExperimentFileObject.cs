using CodeHelper;
using ODMR_Lab.IO操作;
using ODMR_Lab.数据处理;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODMR_Lab
{
    public abstract class ExperimentFileObject<ParamType, ConfigType>
        where ParamType : ExpParamBase
        where ConfigType : ConfigBase
    {
        /// <summary>
        /// 非法字符
        /// </summary>
        public static List<char> InvaliidChars = new List<char>() { '$' };

        /// <summary>
        /// 实验文件类型
        /// </summary>
        public abstract ExperimentFileTypes ExpType { get; protected set; }

        /// <summary>
        /// 实验参数
        /// </summary>
        public abstract ParamType Param { get; set; }

        /// <summary>
        /// UI显示参数
        /// </summary>
        public abstract ConfigType Config { get; set; }

        public bool ReadFromFile(string filepath)
        {
            FileObject obj = FileObject.ReadFromFile(filepath);
            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                throw new Exception("此文件不属于实验类型文件");
            }

            if (ExpType == (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), obj.Descriptions["实验类型"]))
            {
                InnerRead(obj);

                if (Param != null)
                {
                    Param.ReadFromDescription(obj.Descriptions);
                }
                if (Config != null)
                {
                    Config.ReadFromDescription(obj.Descriptions);
                }

                return true;
            }
            return false;
        }

        public bool ReadFromExplorer()
        {
            FileObject obj = FileObject.FindFileFromExplorer();
            if (obj == null) return false;
            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                throw new Exception("此文件不属于实验类型文件");
            }

            if (ExpType == (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), obj.Descriptions["实验类型"]))
            {
                InnerRead(obj);

                if (Param != null)
                {
                    Param.ReadFromDescription(obj.Descriptions);
                }

                if (Config != null)
                {
                    Config.ReadFromDescription(obj.Descriptions);
                }

                return true;
            }
            return false;
        }

        public void WriteToFile(string filefolder, string filename)
        {
            if (!Directory.Exists(filefolder))
            {
                Directory.CreateDirectory(filefolder);
            }
            FileObject obj = InnerWrite();

            if (Param != null)
            {

                Dictionary<string, string> dic = Param.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }
            if (Config != null)
            {

                Dictionary<string, string> dic = Config.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }

            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
            }

            obj.SaveToFile(Path.Combine(filefolder, filename));
        }

        public bool WriteFromExplorer(string defaultName = "")
        {
            FileObject obj = InnerWrite();

            if (Param != null)
            {
                Dictionary<string, string> dic = Param.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }

            if (Config != null)
            {
                Dictionary<string, string> dic = Config.GenerateDescription();
                foreach (var item in dic)
                {
                    if (!obj.Descriptions.Keys.Contains(item.Key))
                    {
                        obj.Descriptions.Add(item.Key, item.Value);
                    }
                }
            }


            if (!obj.Descriptions.Keys.Contains("实验类型"))
            {
                obj.Descriptions.Add("实验类型", Enum.GetName(typeof(ExperimentFileTypes), ExpType));
            }
            return obj.SaveFileFromExplorer(defaultname: defaultName);
        }

        /// <summary>
        /// 获取文件的实验类型
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static ExperimentFileTypes GetExpType(string filepath)
        {
            Dictionary<string, string> dic = FileObject.ReadDescription(filepath);
            if (!dic.Keys.Contains("实验类型"))
            {
                return ExperimentFileTypes.None;
            }
            try
            {
                return (ExperimentFileTypes)Enum.Parse(typeof(ExperimentFileTypes), dic["实验类型"]);
            }
            catch (Exception ex)
            {
                return ExperimentFileTypes.None;
            }
        }

        /// <summary>
        /// 内部读操作，将obj中的信息转化成对应的ExperimentFileObject
        /// </summary>
        /// <param name="fobj"></param>
        protected abstract void InnerRead(FileObject fobj);

        /// <summary>
        /// 内部写操作，将ExperimentFileObject中的信息转化成FileObject
        /// </summary>
        /// <returns></returns>
        protected abstract FileObject InnerWrite();

        /// <summary>
        /// 转换成数据可视化对象
        /// </summary>
        /// <returns></returns>
        public abstract DataVisualSource ToDataVisualSource();

    }
}

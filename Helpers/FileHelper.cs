using Controls.Windows;
using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab
{
    public class FileHelper
    {
        /// <summary>
        /// 去除掉路径中的所有非法字符
        /// </summary>
        /// <param name="rawstr"></param>
        /// <returns></returns>
        public static string ProcessPathStr(string pathstr)
        {
            var chars = Path.GetInvalidPathChars();
            foreach (char c in chars)
            {
                pathstr = pathstr.Replace(c.ToString(), "");
            }
            return pathstr;
        }

        /// <summary>
        /// 去除掉路径中的所有非法字符
        /// </summary>
        /// <param name="rawstr"></param>
        /// <returns></returns>
        public static string ProcessFileStr(string filenamestr)
        {
            var chars = Path.GetInvalidFileNameChars();
            foreach (char c in chars)
            {
                filenamestr = filenamestr.Replace(c.ToString(), "");
            }
            return filenamestr;
        }

        /// <summary>
        /// 将字符串用特定字符连接起来
        /// </summary>
        /// <param name="combinestr"></param>
        /// <param name="strs"></param>
        /// <returns></returns>
        public static string Combine(string combinestr, params string[] strs)
        {
            string newstr = "";
            for (int i = 0; i < strs.Length; i++)
            {
                if (i < strs.Length - 1)
                    newstr += strs[i] + combinestr;
                else
                {
                    newstr += strs[i];
                }
            }
            return newstr;
        }

        /// <summary>
        /// 将字符串用特定字符连接起来
        /// </summary>
        /// <param name="combinestr"></param>
        /// <param name="strs"></param>
        /// <returns></returns>
        public static string Combine(string combinestr, List<string> strs)
        {
            string newstr = "";
            for (int i = 0; i < strs.Count; i++)
            {
                if (i < strs.Count - 1)
                    newstr += strs[i] + combinestr;
                else
                {
                    newstr += strs[i];
                }
            }
            return newstr;
        }

        /// <summary>
        /// 获取路径中的最后一级目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetLastFolder(string path)
        {
            return path.Split(Path.DirectorySeparatorChar).Last();
        }
    }
}

using CodeHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace ODMR_Lab.数据记录
{
    public class NoteAssemble
    {

        public int FileIndex { get; set; } = -1;
        /// <summary>
        /// 记录集合名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 笔记集合路径
        /// </summary>
        public string RootFolderPath { get; set; } = "";

        /// <summary>
        /// 全局标签
        /// </summary>
        public List<NoteTag> GlobalTags { get; set; } = new List<NoteTag>();

        public List<Note> Notes { get; set; } = new List<Note>();

        public NoteAssemble(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 列举已有的文件号
        /// </summary>
        public List<int> EnumerateExistedFileIndexes()
        {
            var dirs = Directory.GetDirectories(RootFolderPath);
            List<int> nums = new List<int>();
            foreach (var dir in dirs)
            {
                try
                {
                    nums.Add(int.Parse(FileHelper.GetLastFolder(dir).Replace("assemble", "")));
                }
                catch (Exception)
                {
                }
            }
            return nums;
        }

        /// <summary>
        /// 生成新的文件序号
        /// </summary>
        /// <returns></returns>
        public int GenerateNewFileIndex()
        {
            var inds = EnumerateExistedFileIndexes();
            if (inds.Count == 0) return 0;
            return inds.Max() + 1;
        }

        /// <summary>
        /// 获取笔记本的文件夹路径
        /// </summary>
        /// <returns></returns>
        public string GetAssembleFolderPath()
        {
            return Path.Combine(RootFolderPath, "assemble" + FileIndex.ToString());
        }

        /// <summary>
        /// 从文件中查找记录本并加载到程序中
        /// </summary>
        public void Load()
        {
            string assemblefolder = GetAssembleFolderPath();
            if (RootFolderPath == "" || !Directory.Exists(assemblefolder)) return;
            Notes.Clear();
            //获取所note
            var infos = Directory.GetDirectories(assemblefolder);
            foreach (var info in infos)
            {
                Note note = new Note(info, this);
                Notes.Add(note);
            }
            //获取所有标签
            FileObject tagobj = FileObject.ReadFromFile(Path.Combine(assemblefolder, "data.userdat"));
            Name = tagobj.Descriptions["Name"];
            GlobalTags = NoteHelper.ReadTags(tagobj);
            foreach (var item in GlobalTags)
            {
                item.IsPrivate = true;
            }
        }

        /// <summary>
        /// 创建记录本目录，如果已经存在则覆盖
        /// </summary>
        /// <param name="path"></param>
        public void ChangeAssembleFile()
        {
            if (!Directory.Exists(GetAssembleFolderPath()))
                Directory.CreateDirectory(GetAssembleFolderPath());
            FileObject obj = new FileObject();
            obj.Descriptions.Add("Name", Name);
            NoteHelper.WriteTags(obj, GlobalTags);
            obj.SaveToFile(Path.Combine(GetAssembleFolderPath(), "data.userdat"));
            ///更新所有子标签文件
            foreach (var item in Notes)
            {
                item.ParentTags = NoteHelper.GetNewNoteParentTags(item.ParentTags, GlobalTags.Select((x) => x.GetOutlineCopy()).ToList());
                FileObject parentobj = new FileObject();
                NoteHelper.WriteTags(parentobj, item.ParentTags);
                parentobj.SaveToFile(Path.Combine(item.GetNoteFolderPath(), "parenttagdata.userdat"));
            }
        }
    }
}

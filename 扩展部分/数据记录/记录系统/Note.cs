using CodeHelper;
using Controls.Windows;
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
    public class Note
    {
        public int FileIndex { get; set; } = -1;

        public string Name { get; set; } = "";

        public List<NoteUnit> NoteContents { get; set; } = new List<NoteUnit>();

        public NoteAssemble Parent { get; set; } = null;

        /// <summary>
        /// 全局标签
        /// </summary>
        public List<NoteTag> GlobalTags { get; set; } = new List<NoteTag>();

        public List<NoteTag> InnerTags = new List<NoteTag>();

        public List<NoteTag> ParentTags = new List<NoteTag>();

        public Note(NoteAssemble parent, string name)
        {
            Parent = parent;
            Name = name;
        }

        public Note(string noteDirePath, NoteAssemble parent)
        {
            FileIndex = int.Parse(NoteHelper.GetLastFolder(noteDirePath).Replace("note", ""));
            Parent = parent;
            FileObject obj = FileObject.ReadFromFile(Path.Combine(noteDirePath, "data.userdat"));
            FileObject gtagobj = FileObject.ReadFromFile(Path.Combine(noteDirePath, "globaltagdata.userdat"));
            FileObject ptagobj = FileObject.ReadFromFile(Path.Combine(noteDirePath, "parenttagdata.userdat"));
            Name = obj.Descriptions["Name"];
            LoadUnits();
            ///内部标签
            InnerTags = NoteHelper.ReadTags(obj);
            foreach (var item in InnerTags)
            {
                item.IsPrivate = true;
            }
            ///父级标签
            ParentTags = NoteHelper.ReadTags(ptagobj);
            foreach (var item in ParentTags)
            {
                item.IsPrivate = false;
            }
            ///全局标签
            GlobalTags = NoteHelper.ReadTags(gtagobj);
            foreach (var item in GlobalTags)
            {
                item.IsPrivate = true;
            }
        }

        /// <summary>
        /// 从笔记路径中加载记录
        /// </summary>
        public void LoadUnits()
        {
            var infos = Directory.GetDirectories(GetNoteFolderPath());
            foreach (var info in infos)
            {
                NoteUnit unit = new NoteUnit(this, info);
                NoteContents.Add(unit);
            }
        }

        /// <summary>
        /// 列举已有的文件号
        /// </summary>
        public List<int> EnumerateExistedFileIndexes()
        {
            var dirs = Directory.GetDirectories(Parent.GetAssembleFolderPath());
            List<int> nums = new List<int>();
            foreach (var dir in dirs)
            {
                try
                {
                    nums.Add(int.Parse(NoteHelper.GetLastFolder(dir).Replace("note", "")));
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
        /// 获取笔记的文件夹路径
        /// </summary>
        /// <returns></returns>
        public string GetNoteFolderPath()
        {
            return System.IO.Path.Combine(Parent.GetAssembleFolderPath(), NoteHelper.ProcessPathStr("note" + FileIndex.ToString()));
        }


        #region 文件操作
        /// <summary>
        /// 在文件中修改记录,如果没有则创建新的文件夹
        /// </summary>
        public void ChangeNoteInFile()
        {
            if (!Directory.Exists(GetNoteFolderPath()))
                Directory.CreateDirectory(GetNoteFolderPath());
            FileObject obj = new FileObject();
            //创建信息文件
            obj.Descriptions.Add("Name", Name);
            NoteHelper.WriteTags(obj, InnerTags);
            obj.SaveToFile(Path.Combine(GetNoteFolderPath(), "data.userdat"));
            FileObject gobj = new FileObject();
            NoteHelper.WriteTags(gobj, GlobalTags);
            gobj.SaveToFile(Path.Combine(GetNoteFolderPath(), "globaltagdata.userdat"));
            FileObject pobj = new FileObject();
            NoteHelper.WriteTags(pobj, ParentTags);
            pobj.SaveToFile(Path.Combine(GetNoteFolderPath(), "parenttagdata.userdat"));
            //更新所有记录文件
            foreach (var item in NoteContents)
            {
                item.ParentTags = NoteHelper.GetNewNoteParentTags(item.ParentTags, GlobalTags.Select((x) => x.GetOutlineCopy()).ToList());
                FileObject parentobj = new FileObject();
                NoteHelper.WriteTags(parentobj, item.ParentTags);
                parentobj.SaveToFile(Path.Combine(item.GetNoteUnitFolderPath(), "parenttagdata.userdat"));
            }
        }

        /// <summary>
        /// 从文件中删除记录
        /// </summary>
        public void DeleteNoteFromFile()
        {
            Directory.Delete(GetNoteFolderPath(), true);
        }
        #endregion
    }
}

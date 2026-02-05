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
using System.Xml.Linq;
using Path = System.IO.Path;

namespace ODMR_Lab.数据记录
{
    public class NoteUnit
    {
        /// <summary>
        /// 记录时间
        /// </summary>
        public DateTime NoteTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 描述内容
        /// </summary>
        public string Description { get; set; } = "";

        public Note Parent { get; set; } = null;

        public NoteUnit(Note ParentNote, string description, DateTime createtime)
        {
            Parent = ParentNote;
            Description = description;
            NoteTime = createtime;
        }

        public NoteUnit(Note ParentNote, string NoteDirePath)
        {
            try
            {
                Parent = ParentNote;
                FileObject obj = FileObject.ReadFromFile(Path.Combine(NoteDirePath, "data.userdat"));
                Description = obj.Descriptions["Description"];
                NoteTime = DateTime.FromOADate(double.Parse(obj.Descriptions["CreateTime"]));
                InnerTags = NoteHelper.ReadTags(obj);
                foreach (var item in InnerTags)
                {
                    item.IsPrivate = true;
                }
            }
            catch (Exception)
            {
            }
            try
            {
                FileObject obj = FileObject.ReadFromFile(Path.Combine(NoteDirePath, "parenttagdata.userdat"));
                ParentTags = NoteHelper.ReadTags(obj);
                foreach (var item in ParentTags)
                {
                    item.IsPrivate = false;
                }
            }
            catch (Exception)
            {
            }
        }

        public List<NoteTag> InnerTags = new List<NoteTag>();

        public List<NoteTag> ParentTags = new List<NoteTag>();

        public int GetFileCount()
        {
            int count = 0;
            var files = Directory.GetFiles(GetNoteUnitFolderPath());
            foreach (var item in files)
            {
                if (Path.GetExtension(item) == ".png" || Path.GetExtension(item) == ".pdf" || Path.GetExtension(item) == ".jpg") ++count;
            }
            return count;
        }

        public string GetNoteUnitFolderPath()
        {
            return System.IO.Path.Combine(Parent.GetNoteFolderPath(), NoteHelper.ProcessPathStr(NoteTime.ToString("yyyy-MM-dd-HH-mm-ss")));
        }

        #region 文件操作
        /// <summary>
        /// 从文件中删除记录
        /// </summary>
        public void DeleteNoteUnitFromFile()
        {
            Directory.Delete(GetNoteUnitFolderPath(), true);
        }

        /// <summary>
        /// 在文件中修改记录
        /// </summary>
        public void ChangeNoteUnitInFile()
        {
            if (!Directory.Exists(GetNoteUnitFolderPath()))
            {
                Directory.CreateDirectory(GetNoteUnitFolderPath());
            }
            FileObject obj = new FileObject();
            //创建信息文件
            obj.Descriptions.Add("Description", Description);
            obj.Descriptions.Add("CreateTime", NoteTime.ToOADate().ToString());
            NoteHelper.WriteTags(obj, InnerTags);
            obj.SaveToFile(Path.Combine(GetNoteUnitFolderPath(), "data.userdat"));
            obj = new FileObject();
            NoteHelper.WriteTags(obj, ParentTags);
            obj.SaveToFile(Path.Combine(GetNoteUnitFolderPath(), "parenttagdata.userdat"));
        }
        #endregion
    }
}

using CodeHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace ODMR_Lab.数据记录
{
    public class NoteHelper
    {

        public static List<NoteTag> ReadTags(FileObject obj)
        {
            var puretags = obj.GetDataNames().Where((x) => x.Contains("PureTextTag"));
            var captiontags = obj.GetDataNames().Where((x) => x.Contains("CaptionTextTag"));
            var singleoptags = obj.GetDataNames().Where((x) => x.Contains("SingleOptionTag"));
            var multioptagindexs = obj.GetDataNames().Where((x) => x.Contains("MultiOptionTag"));

            List<NoteTag> tags = new List<NoteTag>();

            foreach (var tag in puretags)
            {
                PureTextTag tag1 = new PureTextTag();
                tag1.Content = obj.ExtractString(tag)[0];
                tag1.TagColor = (Color)ColorConverter.ConvertFromString(tag.Split('♠')[1]);
                tags.Add(tag1);
            }

            foreach (var tag in captiontags)
            {
                CaptionTextTag tag1 = new CaptionTextTag();
                tag1.Content = obj.ExtractString(tag)[0];
                tag1.TagColor = (Color)ColorConverter.ConvertFromString(tag.Split('♠')[1]);
                tag1.Title = tag.Split('♠')[2];
                tags.Add(tag1);
            }

            foreach (var tag in singleoptags)
            {
                SingleOptionTag tag1 = new SingleOptionTag();
                tag1.Options = obj.ExtractString(tag);
                tag1.TagColor = (Color)ColorConverter.ConvertFromString(tag.Split('♠')[1]);
                tag1.OptionIndex = int.Parse(tag.Split('♠')[2]);
                tag1.Title = tag.Split('♠')[3];
                tags.Add(tag1);
            }

            foreach (var tag in multioptagindexs)
            {
                MultiOptionTag tag1 = new MultiOptionTag();
                tag1.Options = obj.ExtractString(tag);
                var splits = tag.Split('♠').ToList();
                tag1.TagColor = (Color)ColorConverter.ConvertFromString(splits[1]);
                tag1.Title = tag.Split('♠')[2];
                List<int> inds = new List<int>();
                for (int i = 3; i < splits.Count; i++)
                {
                    tag1.OptionIndex.Add(int.Parse(splits[i]));
                }
                tags.Add(tag1);
            }

            return tags;
        }

        public static void WriteTags(FileObject obj, List<NoteTag> tags)
        {
            foreach (var item in tags)
            {
                if (item is PureTextTag)
                {
                    obj.WriteStringData(FileHelper.Combine("♠", "PureTextTag", item.TagColor.ToString()), new List<string>() { (item as PureTextTag).Content });
                }
                if (item is CaptionTextTag)
                {
                    obj.WriteStringData(FileHelper.Combine("♠", "CaptionTextTag", item.TagColor.ToString(), (item as CaptionTextTag).Title), new List<string>() { (item as CaptionTextTag).Content });
                }
                if (item is SingleOptionTag)
                {
                    obj.WriteStringData(FileHelper.Combine("♠", "SingleOptionTag", item.TagColor.ToString(), (item as SingleOptionTag).OptionIndex.ToString(), (item as SingleOptionTag).Title), (item as SingleOptionTag).Options);
                }
                if (item is MultiOptionTag)
                {
                    obj.WriteStringData(FileHelper.Combine("♠", "MultiOptionTag", item.TagColor.ToString(), (item as MultiOptionTag).Title, FileHelper.Combine("♠", (item as MultiOptionTag).OptionIndex.Select(x => x.ToString()).ToList())), (item as MultiOptionTag).Options);
                }
            }
        }

        public static List<NoteTag> GetNewNoteParentTags(List<NoteTag> oldtags, List<NoteTag> newTags)
        {
            foreach (var item in newTags)
            {
                var existedtags = oldtags.Where((x) => item.IsSameAs(x));
                if (existedtags.Count() != 0)
                {
                    existedtags.ElementAt(0).CopyValueTo(item);
                }
            }
            return newTags;
        }
    }
}

using NetTopologySuite.Index.HPRtree;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ODMR_Lab.数据记录
{
    public abstract class NoteTag
    {
        /// <summary>
        /// 标签颜色
        /// </summary>
        public Color TagColor { get; set; } = Colors.Transparent;

        public abstract void CopyOutlineTo(NoteTag target);

        public abstract NoteTag GetOutlineCopy();

        public abstract void CopyValueTo(NoteTag target);

        public abstract NoteTag GetValueCopy();

        public abstract void FullCopyTo(NoteTag target);

        public abstract NoteTag GetFullCopy();

        public abstract bool IsSameAs(NoteTag target);

        public bool IsPrivate { get; set; } = false;
    }

    public class PureTextTag : NoteTag
    {
        public string Content { get; set; } = "";

        public override void CopyOutlineTo(NoteTag target)
        {
            try
            {
                (target as PureTextTag).Content = Content;
                (target as PureTextTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetOutlineCopy()
        {
            try
            {
                PureTextTag target = new PureTextTag();
                target.Content = Content;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void CopyValueTo(NoteTag target)
        {
            try
            {
                (target as PureTextTag).Content = Content;
                (target as PureTextTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetValueCopy()
        {
            try
            {
                PureTextTag target = new PureTextTag();
                target.Content = Content;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void FullCopyTo(NoteTag target)
        {
            try
            {
                (target as PureTextTag).Content = Content;
                (target as PureTextTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetFullCopy()
        {
            try
            {
                PureTextTag target = new PureTextTag();
                target.Content = Content;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override bool IsSameAs(NoteTag target)
        {
            if (target is PureTextTag)
            {
                if ((target as PureTextTag).Content == Content) return true;
            }
            return false;
        }
    }

    public class CaptionTextTag : NoteTag
    {
        public string Title { get; set; } = "";

        public string Content { get; set; } = "";

        public override void CopyOutlineTo(NoteTag target)
        {
            try
            {
                (target as CaptionTextTag).Title = Title;
                (target as CaptionTextTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetOutlineCopy()
        {
            try
            {
                CaptionTextTag target = new CaptionTextTag();
                target.Title = Title;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override void CopyValueTo(NoteTag target)
        {
            try
            {
                (target as CaptionTextTag).Content = Content;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetValueCopy()
        {
            try
            {
                CaptionTextTag target = new CaptionTextTag();
                target.Content = Content;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override void FullCopyTo(NoteTag target)
        {
            try
            {
                (target as CaptionTextTag).Title = Title;
                (target as CaptionTextTag).Content = Content;
                (target as CaptionTextTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }

        public override NoteTag GetFullCopy()
        {
            try
            {
                CaptionTextTag target = new CaptionTextTag();
                target.Title = Title;
                target.Content = Content;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override bool IsSameAs(NoteTag target)
        {
            if (target is CaptionTextTag)
            {
                if ((target as CaptionTextTag).Title == Title) return true;
            }
            return false;
        }
    }

    public abstract class OptionTag : NoteTag
    {
        public List<string> Options { get; set; } = new List<string>();

        public string Title { get; set; } = "";
    }

    public class MultiOptionTag : OptionTag
    {
        /// <summary>
        /// 选项序号
        /// </summary>
        public List<int> OptionIndex { get; set; } = new List<int>();

        public override void CopyOutlineTo(NoteTag target)
        {
            try
            {
                (target as MultiOptionTag).Options = Options.ToArray().ToList();
                (target as MultiOptionTag).Title = Title;
               (target as MultiOptionTag).TagColor = TagColor;
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetOutlineCopy()
        {
            try
            {
                MultiOptionTag target = new MultiOptionTag();
                target.Options = Options.ToArray().ToList();
                target.Title = Title;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void CopyValueTo(NoteTag target)
        {
            try
            {
                (target as MultiOptionTag).OptionIndex = OptionIndex.ToArray().ToList();
                for (int i = 0; i < OptionIndex.Count; i++)
                {
                    if (OptionIndex[i] < 0 || OptionIndex[i] > Options.Count - 1)
                    {
                        OptionIndex.RemoveAt(i);
                        --i;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetValueCopy()
        {
            try
            {
                MultiOptionTag target = new MultiOptionTag();
                target.OptionIndex = OptionIndex.ToArray().ToList();
                for (int i = 0; i < target.OptionIndex.Count; i++)
                {
                    if (target.OptionIndex[i] < 0 || target.OptionIndex[i] > target.Options.Count - 1)
                    {
                        target.OptionIndex.RemoveAt(i);
                        --i;
                    }
                }
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void FullCopyTo(NoteTag target)
        {
            try
            {
                (target as MultiOptionTag).Options = Options.ToArray().ToList();
                (target as MultiOptionTag).OptionIndex = OptionIndex.ToArray().ToList();
                (target as MultiOptionTag).Title = Title;
                (target as MultiOptionTag).TagColor = TagColor;
                for (int i = 0; i < OptionIndex.Count; i++)
                {
                    if (OptionIndex[i] < 0 || OptionIndex[i] > Options.Count - 1)
                    {
                        OptionIndex.RemoveAt(i);
                        --i;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetFullCopy()
        {
            try
            {
                MultiOptionTag target = new MultiOptionTag();
                target.OptionIndex = OptionIndex.ToArray().ToList();
                target.Options = Options.ToArray().ToList();
                target.Title = Title;
                target.TagColor = TagColor;
                for (int i = 0; i < target.OptionIndex.Count; i++)
                {
                    if (target.OptionIndex[i] < 0 || target.OptionIndex[i] > target.Options.Count - 1)
                    {
                        target.OptionIndex.RemoveAt(i);
                        --i;
                    }
                }
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override bool IsSameAs(NoteTag target)
        {
            if (target is MultiOptionTag)
            {
                if ((target as MultiOptionTag).Title == Title) return true;
            }
            return false;
        }
    }

    public class SingleOptionTag : OptionTag
    {
        /// <summary>
        /// 选项序号
        /// </summary>
        public int OptionIndex { get; set; }

        public override void CopyOutlineTo(NoteTag target)
        {
            try
            {
                (target as SingleOptionTag).Options = Options.ToArray().ToList();
                (target as SingleOptionTag).TagColor = TagColor;
                (target as SingleOptionTag).Title = Title;
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetOutlineCopy()
        {
            try
            {
                SingleOptionTag target = new SingleOptionTag();
                target.Options = Options.ToArray().ToList();
                target.Title = Title;
                target.TagColor = TagColor;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void CopyValueTo(NoteTag target)
        {
            try
            {
                (target as SingleOptionTag).OptionIndex = OptionIndex;
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetValueCopy()
        {
            try
            {
                SingleOptionTag target = new SingleOptionTag();
                target.OptionIndex = OptionIndex;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override void FullCopyTo(NoteTag target)
        {
            try
            {
                (target as SingleOptionTag).Options = Options.ToArray().ToList();
                (target as SingleOptionTag).OptionIndex = OptionIndex;
                (target as SingleOptionTag).TagColor = TagColor;
                (target as SingleOptionTag).Title = Title;
            }
            catch (Exception)
            {
            }
        }
        public override NoteTag GetFullCopy()
        {
            try
            {
                SingleOptionTag target = new SingleOptionTag();
                target.Options = Options.ToArray().ToList();
                target.OptionIndex = OptionIndex;
                target.TagColor = TagColor;
                target.Title = Title;
                return target;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public override bool IsSameAs(NoteTag target)
        {
            if (target is SingleOptionTag)
            {
                if ((target as SingleOptionTag).Title == Title) return true;
            }
            return false;
        }
    }
}

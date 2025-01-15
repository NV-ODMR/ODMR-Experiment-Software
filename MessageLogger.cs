using ODMR_Lab.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ODMR_Lab
{
    public class MessageLogger
    {
        public static List<Message> Messages = new List<Message>();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="Message"></param>
        /// <param name=""></param>
        public static void AddLogger(string part, string message, MessageTypes type, bool ShowMessageBox = false, bool Log = true, Window owner = null)
        {
            if (Log)
            {
                Messages.Add(new ODMR_Lab.Message(type, part, message));
            }
            if (ShowMessageBox)
            {
                MessageWindow.ShowTipWindow(message, owner);
            }
        }
    }

    /// <summary>
    /// 信息记录
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 信息种类
        /// </summary>
        public MessageTypes MessageType { get; private set; } = MessageTypes.Information;

        public string Information { get; private set; } = "";

        /// <summary>
        /// 所属部分
        /// </summary>
        public string Part { get; private set; } = "";

        public DateTime Timestamp { get; private set; }

        public Message(MessageTypes type, string belongPart, string information)
        {
            MessageType = type;
            Part = belongPart;
            Information = information;
            Timestamp = DateTime.Now;
        }
    }

    public enum MessageTypes
    {
        /// <summary>
        /// 提示
        /// </summary>
        Information = 0,
        /// <summary>
        /// 警告
        /// </summary>
        Warning = 1
    }
}

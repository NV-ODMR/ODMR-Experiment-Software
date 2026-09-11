using System;
using System.Collections.Generic;

namespace ODMR_Lab
{
    public class MessageLogger
    {
        private static readonly List<Message> _messages = new List<Message>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void LogInfo(string message, string part = "System")
        {
            lock (_lock)
            {
                var msg = new Message(MessageTypes.Information, part, message);
                _messages.Add(msg);
                Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} [{part}] {message}");
            }
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void LogError(string message, string part = "System")
        {
            lock (_lock)
            {
                var msg = new Message(MessageTypes.Warning, part, message);
                _messages.Add(msg);
                Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} [{part}] {message}");
            }
        }

        /// <summary>
        /// 获取最近的日志
        /// </summary>
        public static List<Message> GetRecentLogs(int count = 100)
        {
            lock (_lock)
            {
                if (_messages.Count <= count)
                    return new List<Message>(_messages);
                return _messages.GetRange(_messages.Count - count, count);
            }
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public static void ClearLogs()
        {
            lock (_lock)
            {
                _messages.Clear();
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

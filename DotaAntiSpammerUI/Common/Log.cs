using System;
using System.Collections.Generic;
using System.Linq;

namespace DotaAntiSpammerNet.Common
{
    public enum LogLevel
    {
        Normal,
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    }

    public interface ILogger
    {
        void WriteLine(string line);
    }

    public static class Log
    {
        static Log()
        {
            Loggers = new Dictionary<string, ILogger>();
            LogFormatterFunc = DefaultFormatter;
            Loggers.Add("File", FileLogger.Instance);
        }

        public static Func<LogLevel, string, object[], string> LogFormatterFunc { get; set; }

        private static Dictionary<string, ILogger> Loggers { get; }

        public static void Register(string publicKey, ILogger logger)
        {
            Loggers.Add(publicKey, logger);
        }

        public static bool Unregister(string publicKey)
        {
            return Loggers.Remove(publicKey);
        }

        public static ILogger Get(string key)
        {
            return !Loggers.ContainsKey(key) ? Loggers["File"] : Loggers[key];
        }

        public static void WriteLine(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Normal, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Debug, format, args);
        }

        public static void Info(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Information, format, args);
        }

        public static void Warn(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Warning, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Error, format, args);
        }

        public static void Fatal(string format, params object[] args)
        {
            WriteLogEntry(LogLevel.Fatal, format, args);
        }

        private static void WriteLogEntry(LogLevel logLevel, string format, params object[] args)
        {
            var formatted = LogFormatterFunc(logLevel, format, args);

            foreach (var logger in Loggers.ToArray()) logger.Value.WriteLine(formatted);
        }

        private static string DefaultFormatter(LogLevel type, string format, params object[] args)
        {
            if (args != null && args.Any()) format = string.Format(format, args);

            var time = DateTime.Now;
            var logType = type == LogLevel.Normal ? "" : $"[{type}]";

            return $"{logType}[{time}] {format}";
        }
    }
}
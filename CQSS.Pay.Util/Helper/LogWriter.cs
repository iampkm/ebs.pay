using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// 文本日志记录
    /// </summary>
    public class LogWriter
    {
        private static readonly string _LogPathTemplate = @"{0}Logs\{2}\{1:yyyy\\M}\";
        private static readonly string _LogFilenameTemplate = "{0:yyyy-MM-dd-HH}.log";
        private static readonly string _LogFilePath = System.Configuration.ConfigurationManager.AppSettings["LogFilePath"];

        /// <summary>
        /// 同步锁定对象
        /// </summary>
        private static object _sync = new object();
        private static string _appName;
        private static string _basePath;
        private static string _defaultLogDirectoryName = "System";
        private static DateTime _lastSweepTime = DateTime.Now;
        private struct OpenedFile
        {
            public string Filename;
            public DateTime LastWriteTime;
            public TextWriter Writer;
        }
        private static Dictionary<string, OpenedFile> _files = new Dictionary<string, OpenedFile>();

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        public static void WriteLog(string log)
        {
            WriteLog(log, ExceptionHelper.ExceptionLevel.Infomation);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="eLevel"></param>
        public static void WriteLog(string log, ExceptionHelper.ExceptionLevel eLevel)
        {
            Write(log, _defaultLogDirectoryName, true, eLevel, _LogPathTemplate, _LogFilenameTemplate);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="eLevel"></param>
        /// <param name="parameters"></param>
        public static void WriteLog(string log, ExceptionHelper.ExceptionLevel eLevel, params object[] parameters)
        {
            WriteLog(string.Format(log, parameters), eLevel);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logDir"></param>
        public static void WriteLog(string log, string logDir)
        {
            WriteLog(log, logDir, ExceptionHelper.ExceptionLevel.None);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logDir"></param>
        /// <param name="eLevel"></param>
        public static void WriteLog(string log, string logDir, ExceptionHelper.ExceptionLevel eLevel)
        {
            WriteLog(log, logDir, true, eLevel, _LogPathTemplate, _LogFilenameTemplate);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logDir"></param>
        /// <param name="logTime"></param>
        /// <param name="eLevel"></param>
        public static void WriteLog(string log, string logDir, bool logTime, ExceptionHelper.ExceptionLevel eLevel)
        {
            WriteLog(log, logDir, logTime, eLevel, _LogPathTemplate, _LogFilenameTemplate);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logDir"></param>
        /// <param name="logTime"></param>
        /// <param name="eLevel"></param>
        /// <param name="filenameTemplate">
        /// {0}: 当前时间；
        /// 默认值为"{0:yyyy-MM-dd-HH}.log"
        /// </param>
        /// <param name="pathTemplate">
        /// {0}: 程序的基础路径；
        /// {1}: 当前时间；
        /// {2}: 记录的日志文件夹；
        /// {3}: 应用程序的名字；
        /// 默认值为@"{0}Logs\{2}\{1:yyyy\\M}\"
        /// </param>
        public static void WriteLog(string log, string logDir, bool logTime, ExceptionHelper.ExceptionLevel eLevel, string pathTemplate, string filenameTemplate)
        {
            Write(log, logDir, logTime, eLevel, pathTemplate, filenameTemplate);
        }

        /// <summary>
        /// 写入日志文件，该操作是线程安全的
        /// </summary>
        /// <param name="filenameTemplate"></param>
        /// <param name="pathTemplate"></param>
        /// <param name="log"></param>
        /// <param name="logDir"></param>
        /// <param name="logTime"></param>
        /// <param name="eLevel"></param>
        private static void Write(string log, string logDir, bool logTime, ExceptionHelper.ExceptionLevel eLevel, string pathTemplate, string filenameTemplate)
        {
            try
            {
                Monitor.Enter(_sync);
                if (string.IsNullOrEmpty(_appName))
                {
                    FileInfo fi = new FileInfo(Process.GetCurrentProcess().MainModule.ModuleName);
                    try
                    {
                        _appName = fi.Name.Remove(fi.Name.Length - fi.Extension.Length);
                    }
                    catch
                    {
                        _appName = "Null";
                    }
                    _basePath = string.IsNullOrWhiteSpace(_LogFilePath) ? AppDomain.CurrentDomain.BaseDirectory : _LogFilePath;
                }
                DateTime curDatetime = DateTime.Now;
                string path = string.Format(string.IsNullOrEmpty(pathTemplate) ? _LogPathTemplate : pathTemplate, _basePath, curDatetime, logDir);
                string filename = String.Format(string.IsNullOrEmpty(filenameTemplate) ? _LogFilenameTemplate : filenameTemplate, curDatetime);
                string strTime = logTime ? curDatetime.ToString("[HH:mm:ss.fff]:") : "";
                WriteIntoFile(String.Format("{3}{2}{0}{1}", strTime, log, ExceptionHelper.GetToken(eLevel), eLevel == ExceptionHelper.ExceptionLevel.None ? "" : "#"), path, filename);
            }
            catch (Exception ex)
            {
                string errMessage = "写入日志文件";
                try
                {
                    DateTime curDatetime = DateTime.Now;
                    string content = String.Format(
                        "{3}{2}{0}{1}",
                        logTime ? curDatetime.ToString("[HH:mm:ss.fff]:") : "",
                        log,
                        ExceptionHelper.GetToken(eLevel),
                        eLevel == ExceptionHelper.ExceptionLevel.None ? "" : "#");
                    string filename = String.Format(string.IsNullOrEmpty(filenameTemplate) ? _LogFilenameTemplate : filenameTemplate, curDatetime);
                    errMessage = string.Format("写[{0}]至日志[{1}]文件", content, filename);
                }
                catch (Exception) { }
                LogExceptionToEventLog(ex, errMessage);
            }
            finally
            {
                Monitor.Exit(_sync);
            }
        }

        /// <summary>
        /// 记录系统日志
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="errMessage"></param>
        private static void LogExceptionToEventLog(Exception ex, string errMessage)
        {
            string exMessage = String.Format("{3}时出现未处理的异常[{2}]\r\n异常信息：{0}\r\n堆栈信息：\r\n{1}", ex.Message, ex.StackTrace, ex.GetType().ToString(), errMessage);
            LogMessageToEventLog(exMessage);
        }

        /// <summary>
        /// 写入系统日志
        /// </summary>
        /// <param name="message"></param>
        private static void LogMessageToEventLog(string message)
        {
            try
            {
                if (!EventLog.SourceExists("LogWriter"))
                {
                    EventLog.CreateEventSource("LogWriter", "Application");
                }
                EventLog.WriteEntry("LogWriter", message);
            }
            catch
            {
            }
        }
        /// <summary>
        /// 日志写入文件
        /// </summary>
        /// <param name="message"></param>
        /// <param name="path"></param>
        /// <param name="filename"></param>
        private static void WriteIntoFile(string message, string path, string filename)
        {
            OpenedFile openFile;
            bool ret = _files.TryGetValue(path.ToLower() + filename, out openFile);
            if (!ret)
            {
                CreateDirectory(path);
                StreamWriter sw = new StreamWriter(path + filename, true);
                TextWriter writer = TextWriter.Synchronized(sw);
                openFile.Filename = path + filename;
                openFile.Writer = writer;
                _files.Add(path.ToLower() + filename, openFile);
            }
            openFile.Writer.WriteLine(message);
            openFile.Writer.Flush();
            DateTime dt = DateTime.Now;
            openFile.LastWriteTime = dt;
            if ((dt - _lastSweepTime).TotalMinutes > 1)
            {
                dt = SweepFiles(dt);
                _lastSweepTime = dt;
            }
        }

        private static DateTime SweepFiles(DateTime dt)
        {
            List<string> keys = new List<string>();
            foreach (var file in _files)
            {
                if ((dt - file.Value.LastWriteTime).TotalMinutes > 1)
                {
                    file.Value.Writer.Flush();
                    file.Value.Writer.Close();
                    file.Value.Writer.Dispose();
                    keys.Add(file.Key);
                }
            }
            foreach (string key in keys)
            {
                _files.Remove(key);
            }
            return dt;
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="directory"></param>
        private static void CreateDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// 异常类
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// 异常等级
        /// </summary>
        public enum ExceptionLevel
        {
            /// <summary>
            /// 警告，简写W
            /// </summary>
            Warning,
            /// <summary>
            /// 信息，简写I
            /// </summary>
            Infomation,
            /// <summary>
            /// 异常，简写E
            /// </summary>
            Exception,
            /// <summary>
            /// 错误，简写!
            /// </summary>
            Error,
            /// <summary>
            /// 无任何异常
            /// </summary>
            None
        }

        /// <summary>
        /// 获取异常的简写字符
        /// </summary>
        /// <param name="eLevel">异常等级</param>
        /// <returns></returns>
        public static string GetToken(ExceptionLevel eLevel)
        {
            switch (eLevel)
            {
                case ExceptionLevel.Warning:
                    return "W";
                case ExceptionLevel.Infomation:
                    return "I";
                case ExceptionLevel.Exception:
                    return "E";
                case ExceptionLevel.Error:
                    return "!";
                case ExceptionLevel.None:
                    return "";
                default:
                    return "?";
            }
        }

    }
}

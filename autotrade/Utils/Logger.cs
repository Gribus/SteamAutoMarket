﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace autotrade.Utils
{
    internal class Logger
    {
        public const string DATE_FROMAT = "HH:mm:ss";

        private static readonly LoggerLevel[] NONE_SHOULD_BE_IGNORED =
            {LoggerLevel.DEBUG, LoggerLevel.INFO, LoggerLevel.ERROR, LoggerLevel.NONE};

        private static readonly LoggerLevel[] DEBUG_SHOULD_BE_IGNORED =
            {LoggerLevel.INFO, LoggerLevel.ERROR, LoggerLevel.NONE};

        private static readonly LoggerLevel[] INFO_SHOULD_BE_IGNORED = {LoggerLevel.ERROR, LoggerLevel.NONE};
        private static readonly LoggerLevel[] ERROR_SHOULD_BE_IGNORED = {LoggerLevel.NONE};

        public static LoggerLevel LOGGER_LEVEL { get; set; } = LoggerLevel.INFO;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void LogToFile(string s)
        {
            Task.Run(() =>
            {
                try
                {
                    File.AppendAllText("log.log", s + "\n");
                }
                catch
                {
                    Thread.Sleep(1000);
                    LogToFile(s);
                }
            });
        }

        public static void Working(string message)
        {
            message = $"{GetCurrentDate()} [WORKING] - {message}";
            LogToLogBox(message);
        }

        public static void Debug(string message)
        {
            if (_ignoreLogs(LoggerLevel.DEBUG)) return;

            message = $"{GetCurrentDate()} [DEBUG] - {message}";
            LogToFile(message);
            LogToLogBox(message);
        }

        public static void Info(string message)
        {
            if (_ignoreLogs(LoggerLevel.INFO)) return;

            message = $"{GetCurrentDate()} [INFO] - {message}";
            LogToFile(message);
            LogToLogBox(message);
        }


        public static void Warning(string message, Exception e = null)
        {
            if (_ignoreLogs(LoggerLevel.INFO)) return;

            message = $"{GetCurrentDate()} [WARN] - {message} " + (e != null ? e.Message + " " + e.StackTrace : "");
            LogToFile(message);
            LogToLogBox(message);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Error(string message, Exception e = null)
        {
            if (_ignoreLogs(LoggerLevel.ERROR)) return;

            message = $"{GetCurrentDate()} [ERROR] - {message}";
            if (e != null) message += $". {e.Message}";

            File.AppendAllText("error.log", message + " " + (e != null ? e.Message + " " + e.StackTrace : "") + "\n");
            LogToLogBox(message);
        }

        public static void Critical(string message, Exception ex)
        {
            message = $"{GetCurrentDate()} [CRITICAL] - {message}. {ex.Message}";
            File.AppendAllText("error.log", $"{message} {ex.StackTrace}\n");

            var confirmResult = MessageBox.Show(message, "Critical unhandled exception",
                MessageBoxButtons.OK, MessageBoxIcon.Stop);

            Application.Exit();
        }

        public static void LogToLogBox(string s)
        {
            if (Program.IsMainThread)
                AppendTextToLogTextBox(s);
            else
                Dispatcher.AsMainForm(() => { AppendTextToLogTextBox(s); });
        }

        private static void AppendTextToLogTextBox(string s)
        {
            var textBox = Program.MainForm.SettingsControlTab.SettingsControl.LogTextBox;
            if (textBox.Lines.Count() > 1000)
            {
                var list = textBox.Lines.ToList();
                list.RemoveRange(0, 500);
                textBox.Lines = list.ToArray();
            }

            textBox.AppendText(s + "\n");
        }

        public static string GetCurrentDate()
        {
            return DateTime.Now.ToString(DATE_FROMAT);
        }

        private static bool _ignoreLogs(LoggerLevel invoked)
        {
            switch (invoked)
            {
                case LoggerLevel.DEBUG: return DEBUG_SHOULD_BE_IGNORED.Contains(invoked);
                case LoggerLevel.INFO: return INFO_SHOULD_BE_IGNORED.Contains(invoked);
                case LoggerLevel.ERROR: return ERROR_SHOULD_BE_IGNORED.Contains(invoked);
                case LoggerLevel.NONE: return NONE_SHOULD_BE_IGNORED.Contains(invoked);
                default: return false;
            }
        }
    }

    internal enum LoggerLevel
    {
        DEBUG,
        INFO,
        ERROR,
        NONE
    }
}
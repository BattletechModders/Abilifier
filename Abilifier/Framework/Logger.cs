using System;
using System.IO;

namespace Abilifier.Framework
{
    public class Logger
    {
        public static StreamWriter logStreamWriter;
        public bool enableLogging;

        public Logger(string modDir, string fileName, bool enableLogging)
        {
            string filePath = Path.Combine(modDir, $"{fileName}.log");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            logStreamWriter = File.AppendText(filePath);
            logStreamWriter.AutoFlush = true;

            this.enableLogging = enableLogging;
        }

        public void LogMessage(string message)
        {
            if (enableLogging)
            {
                string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"INFO: {ts} - {message}");
            }
        }

        public static void LogTrace(string message)
        {
            if (Mod.modSettings.enableTrace)
            {
                string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
                logStreamWriter.WriteLine($"TRACE: {ts} - {message}");
            }
        }

        public static void LogError(string message)
        {
            string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"ERROR: {ts} - {message}");
        }

        public static void LogException(Exception exception)
        {
            string ts = DateTime.UtcNow.ToString("HH:mm:ss.ffff", System.Globalization.CultureInfo.InvariantCulture);
            logStreamWriter.WriteLine($"CRITICAL: {ts} - {exception}");
        }
    }
}
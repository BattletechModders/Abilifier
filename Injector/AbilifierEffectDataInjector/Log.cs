using System;

namespace AbilifierEffectDataInjector
{
    public class Logger
    {
        public void W(string line)
        {
            Console.Write(line);
        }
        public void WL(string line)
        {
            Console.WriteLine(line);
        }
        public void W(int initiation, string line)
        {
            string init = new string(' ', initiation);
            line = init + line; W(line);
        }
        public void WL(int initiation, string line)
        {
            string init = new string(' ', initiation);
            line = init + line; WL(line);
        }
        public void TW(int initiation, string line)
        {
            string init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            W(line);
        }
        public void TWL(int initiation, string line)
        {
            string init = new string(' ', initiation);
            line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "]" + init + line;
            WL(line);
        }
    }
    public static class Log
    {
        private static readonly Logger Logger = new Logger();

        public static Logger Debug
        {
            get
            {
#if DEBUG
                return Logger;
#else
        return null;
#endif
            }
        }

        public static Logger Error => Logger;
    }
}
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public static class Log
    {
        public static RichTextBox box = null;
        private static readonly object _sync = new object();
        private static FileStream fs;

        public static void Init(RichTextBox rtb)
        {
            box = rtb;
            if (File.Exists("log.txt"))
                File.Delete("log.txt");
            fs = File.Create("log.txt");
            Print("Log initialized");
        }

        public static void Print(string s)
        {
            string line = DateTime.Now.ToLongTimeString() + " " + s + "\n";
            lock (_sync)
            {
                byte[] data = Encoding.UTF8.GetBytes(line);
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }
            if (box == null)
                return;
            try
            {
                box.Invoke(new Action(delegate
                {
                    lock (_sync)
                    {
                        box.AppendText(line);
                    }
                }));
            }
            catch { }
        }
    }
}

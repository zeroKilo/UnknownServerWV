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
        private static readonly string log_file_name = "log_backend.txt";

        public static void Init(RichTextBox rtb)
        {
            box = rtb;
            if (File.Exists(log_file_name))
                File.Delete(log_file_name);
            fs = File.Create(log_file_name);
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
                        if (box.Text.Length > 20000)
                            box.Text = box.Text.Substring(box.Text.Length - 19000, 19000);
                    }
                }));
            }
            catch { }
        }
    }
}

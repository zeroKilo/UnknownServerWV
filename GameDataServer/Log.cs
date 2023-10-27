using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GameDataServer
{
    public static class Log
    {
        public static RichTextBox box = null;
        private static readonly object _sync = new object();
        private static readonly string log_file_name = "log_gds.txt";

        public static void Init(RichTextBox rtb)
        {
            box = rtb;
            if (File.Exists(log_file_name))
                File.Delete(log_file_name);
            FileStream fs = File.Create(log_file_name);
            fs.Close();
            Print("Log initialized");
        }

        public static void Print(string s)
        {
            string line = DateTime.Now.ToLongTimeString() + " " + s + "\n";
            lock (_sync)
            {
                File.AppendAllText(log_file_name, line);
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

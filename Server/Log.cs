using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public static class Log
    {
        public static RichTextBox box = null;
        private static readonly object _sync = new object();
        
        public static void Init(RichTextBox rtb)
        {
            box = rtb;
            Print("Log initialized");
        }

        public static void Print(string s)
        {
            if (box == null)
                return;
            try
            {
                box.Invoke(new Action(delegate
                {
                    lock (_sync)
                    {
                        box.AppendText(DateTime.Now.ToLongTimeString() + " " + s + "\n");
                    }
                }));
            }
            catch { }
        }
    }
}

using System;
using System.Text;

namespace GameDataServer
{
    public static class Helper
    {
        public static bool ValidName(string name)
        {
            if (name.Trim() == "")
                return false;
            string allowed = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";
            foreach (char c in name)
                if (allowed.IndexOf(c) == -1)
                    return false;
            return true;
        }
    }
}

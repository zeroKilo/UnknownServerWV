using System;
using System.Text;

namespace GameDataServer
{
    public static class Helper
    {
        public static string Base64Encode(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        public static string Base64Decode(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }

        public static bool ValidName(string name)
        {
            string allowed = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_.";
            foreach (char c in name)
                if (allowed.IndexOf(c) == -1)
                    return false;
            return true;
        }
    }
}

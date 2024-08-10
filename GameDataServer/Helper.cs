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

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}

using System;
using System.IO;

namespace Server
{
    public static class WeaponManager
    {
        public static string weaponSettingsJson = "";
        public static void Init()
        {
            try
            {
                weaponSettingsJson = File.ReadAllText("weapon_settings.json");
                Log.Print("WeaponManager Init Done");
            }
            catch(Exception ex)
            {
                Log.Print("WeaponManager Init Error: " + ex.Message + "\n" + ex.InnerException.Message);
            }
        }
    }
}

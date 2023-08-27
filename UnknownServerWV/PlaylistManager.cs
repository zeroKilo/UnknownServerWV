using System.IO;
using System.Collections.Generic;
using System.Text;
using NetDefines;
using Server;

namespace UnknownServerWV
{
    public class PlaylistManager
    {
        public class PlaylistEntry
        {
            public ServerMode mode;
            public int map;
            public int spawnLoc;
            public int minPlayer;
            public int countDown;
            public int roundTime;
            public int killsToWin;
            public PlaylistEntry()
            {
                mode = ServerMode.BattleRoyaleMode;
                map = spawnLoc = minPlayer = countDown = roundTime = killsToWin = -1;
            }

            public PlaylistEntry(string data)
            {
                string[] parts = data.Split(';');
                mode = (ServerMode)int.Parse(parts[0]);
                map = int.Parse(parts[1]);
                spawnLoc = int.Parse(parts[2]);
                minPlayer = int.Parse(parts[3]);
                countDown = int.Parse(parts[4]);
                roundTime = int.Parse(parts[5]);
                killsToWin = int.Parse(parts[5]);
            }

            public string Save()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append((int)mode + ";");
                sb.Append(map + ";");
                sb.Append(spawnLoc + ";");
                sb.Append(minPlayer + ";");
                sb.Append(countDown + ";");
                sb.Append(roundTime + ";");
                sb.Append(killsToWin);
                return sb.ToString();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Mode: " + mode);
                switch (mode)
                {
                    case ServerMode.Offline:
                        break;
                    case ServerMode.BattleRoyaleMode:
                        sb.Append(" Map: " + BattleRoyaleMode.mapInfos[map].name);
                        sb.Append(" Loc: " + BattleRoyaleMode.mapInfos[map].spawnLocations[spawnLoc]);
                        break;
                    case ServerMode.DeathMatchMode:
                        sb.Append(" Map: " + DeathMatchMode.mapInfos[map].name);
                        sb.Append(" Time: " + roundTime + "min");
                        break;
                    case ServerMode.TeamDeathMatchMode:
                        sb.Append(" Map: " + TeamDeathMatchMode.mapInfos[map].name);
                        sb.Append(" Time: " + roundTime + "min");
                        break;
                    case ServerMode.FreeExploreMode:
                        sb.Append(" Map: " + FreeExploreMode.mapInfos[map].name);
                        sb.Append(" Loc: " + FreeExploreMode.mapInfos[map].spawnLocations[spawnLoc]);
                        sb.Append(" Time: " + roundTime + "min");
                        break;
                }
                return sb.ToString();
            }

            public string GetDetails()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Mode = " + mode);
                switch (mode)
                {
                    case ServerMode.Offline:
                        break;
                    case ServerMode.BattleRoyaleMode:
                        sb.AppendLine("Map = " + BattleRoyaleMode.mapInfos[map].name);
                        sb.AppendLine("Spawn Location = " + BattleRoyaleMode.mapInfos[map].spawnLocations[spawnLoc]);
                        sb.AppendLine("Minimal Player Count = " + minPlayer);
                        sb.AppendLine("Countdown Seconds = " + countDown);
                        sb.AppendLine("Round Time Minutes = " + roundTime);
                        break;
                    case ServerMode.DeathMatchMode:
                        sb.AppendLine("Map = " + DeathMatchMode.mapInfos[map].name);
                        sb.AppendLine("Spawn Location = " + DeathMatchMode.mapInfos[map].spawnLocations[spawnLoc]);
                        sb.AppendLine("Minimal Player Count = " + minPlayer);
                        sb.AppendLine("Countdown Seconds = " + countDown);
                        sb.AppendLine("Kills To Win = " + killsToWin);
                        sb.AppendLine("Round Time Minutes = " + roundTime);
                        break;
                    case ServerMode.TeamDeathMatchMode:
                        sb.AppendLine("Map = " + TeamDeathMatchMode.mapInfos[map].name);
                        sb.AppendLine("Spawn Location = " + TeamDeathMatchMode.mapInfos[map].spawnLocations[spawnLoc]);
                        sb.AppendLine("Minimal Player Count = " + minPlayer);
                        sb.AppendLine("Countdown Seconds = " + countDown);
                        sb.AppendLine("Kills To Win = " + killsToWin);
                        sb.AppendLine("Round Time Minutes = " + roundTime);
                        break;
                    case ServerMode.FreeExploreMode:
                        sb.AppendLine("Map = " + FreeExploreMode.mapInfos[map].name);
                        sb.AppendLine("Spawn Location = " + FreeExploreMode.mapInfos[map].spawnLocations[spawnLoc]);
                        sb.AppendLine("Round Time Minutes = " + roundTime);
                        break;
                }
                return sb.ToString();
            }
        }

        public static string defaultName = "playlist.txt";
        public static List<PlaylistEntry> playlist = new List<PlaylistEntry>();

        public static void Init()
        {
            if (!File.Exists(defaultName))
                File.WriteAllText(defaultName, "#do no edit by hand!");
            string[] lines = File.ReadAllLines(defaultName);
            playlist = new List<PlaylistEntry>();
            foreach(string s in lines)
            {
                if (s.Trim() == "" || s.Trim().StartsWith("#"))
                    continue;
                playlist.Add(new PlaylistEntry(s.Trim()));
            }
        }
    }
}

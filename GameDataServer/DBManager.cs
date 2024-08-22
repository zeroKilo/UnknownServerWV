using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;
using System;
using System.Text;

namespace GameDataServer
{
    public static class DBManager
    {
        public class LoginHistoryEntry
        {
            public long timestamp;
            public int serverId;
            public int userId;
            public LoginHistoryEntry(long timestamp, int serverId, int userId)
            {
                this.timestamp = timestamp;
                this.serverId = serverId;
                this.userId = userId;
            }
        }

        private static string db_path = "";
        private static SQLiteConnection connection = null;
        private static List<PlayerProfile> profiles = new List<PlayerProfile>();
        private static List<GameServer> servers = new List<GameServer>();
        private static readonly object _sync = new object();
        private static readonly object _syncRL = new object();

        private const string table_servers = "servers";
        private const string table_profiles = "profiles";
        private const string table_stats = "stats";
        private const string table_history = "history";

        private static bool needsUpdate = false;

        private static uint playerProfileUpdateCounter = 1;

        private static DateTime lastRLUpdate = DateTime.MinValue;
        private static string lastRLUpdateResult = "";
        public static void Init(string path)
        {
            db_path = path;
            if (!File.Exists(path))
                SQLiteConnection.CreateFile(path);
            connection = new SQLiteConnection("Data Source=" + db_path);
            connection.Open();
            CheckAllTablesPresent();
            Reload(false);
        }

        private static bool CheckAllTablesPresent()
        {
            List<string> names = new List<string>(new string[] { table_servers, table_profiles, table_stats, table_history });
            foreach (string name in names)
            {
                SQLiteDataReader r = ExecuteSQL("SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='" + name + "';");
                r.Read();
                if (r.GetInt32(0) != 1)
                {
                    Log.Print("Cant find table '" + name + "', creating...");
                    CreateNewTable(name);
                }
            }
            return true;
        }

        private static void CreateNewTable(string name)
        {
            string sql;
            switch(name)
            {
                case table_servers:
                    sql = "CREATE TABLE "+ table_servers + " (id INTEGER PRIMARY KEY,public_key TEXT NOT NULL," +
                          "name TEXT NOT NULL,ip TEXT NOT NULL,port_udp TEXT NOT NULL," +
                          "port_tcp TEXT NOT NULL,status TEXT NOT NULL)";
                    break;
                case table_profiles:
                    sql = "CREATE TABLE " + table_profiles + " (id INTEGER PRIMARY KEY,public_key TEXT NOT NULL," +
                          "name TEXT NOT NULL, meta_data TEXT NOT NULL)";
                    break;
                case table_stats:
                    sql = "CREATE TABLE " + table_stats + " (id INTEGER PRIMARY KEY,name TEXT NOT NULL,data TEXT NOT NULL)";
                    break;
                case table_history:
                    sql = "CREATE TABLE " + table_history + " (id INTEGER PRIMARY KEY, timestamp INTEGER, server_id INTEGER, user_id INTEGER)";
                    break;
                default:
                    return;
            }
            ExecuteSimpleSQL(sql);
        }

        private static void LoadAllServers(bool silent = true)
        {
            servers = new List<GameServer>();
            SQLiteDataReader r = ExecuteSQL("SELECT * FROM " + table_servers + " ORDER BY name");
            r.Read();
            while (r.HasRows)
            {
                GameServer gs = new GameServer( r.GetInt32(0), r.GetString(1), r.GetString(2),
                                                r.GetString(3), r.GetString(4), r.GetString(5), 
                                                r.GetString(6));
                servers.Add(gs);
                r.Read();
            }
            if(!silent)
                Log.Print("Loaded " + servers.Count + " server profiles");
        }

        private static void LoadAllProfiles(bool silent = true)
        {
            profiles = new List<PlayerProfile>();
            SQLiteDataReader r = ExecuteSQL("SELECT * FROM " + table_profiles + " ORDER BY name");
            r.Read();
            while (r.HasRows)
            {
                PlayerProfile p = new PlayerProfile(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3));
                profiles.Add(p);
                r.Read();
            }
            if (!silent)
                Log.Print("Loaded " + profiles.Count + " player profiles");
        }

        public static GameServer[] GetServerProfiles()
        {
            List<GameServer> result = new List<GameServer>();
            lock (_sync)
            {
                foreach (GameServer gs in servers)
                    result.Add(gs.Clone());
            }
            return result.ToArray();
        }

        public static PlayerProfile[] GetPlayerProfiles()
        {
            List<PlayerProfile> result = new List<PlayerProfile>();
            lock (_sync)
            {
                foreach (PlayerProfile p in profiles)
                    result.Add(p.Clone());
            }
            return result.ToArray();
        }

        public static List<LoginHistoryEntry> GetLoginsSince(long timestamp)
        {
            List<LoginHistoryEntry> result = new List<LoginHistoryEntry>();
            lock (_sync)
            {
                SQLiteDataReader r = ExecuteSQL("SELECT * FROM " + table_history + " WHERE timestamp >= " + timestamp);
                r.Read();
                while (r.HasRows)
                {
                    result.Add(new LoginHistoryEntry(r.GetInt64(1), r.GetInt32(2), r.GetInt32(3)));
                    r.Read();
                }
                r.Close();
            }
            return result;
        }

        public static string GetRecentLogins(int days = 14)
        {
            lock (_syncRL)
            {
                var hours = (DateTime.Now - lastRLUpdate).TotalHours;
                if (hours < 2 && lastRLUpdateResult != "")
                    return lastRLUpdateResult;
                else
                {
                    Log.Print("HandleGetRecentLogins: refreshing recent logins");
                    lastRLUpdate = DateTime.Now;
                    StringBuilder sb = new StringBuilder();
                    DateTime target = DateTime.Now.AddDays(-1 - days);
                    long timestamp = new DateTimeOffset(target).ToUnixTimeSeconds();
                    List<LoginHistoryEntry> logins = GetLoginsSince(timestamp);
                    GameServer[] listGS = GetServerProfiles();
                    Dictionary<string, List<long>> loginsPerServer = new Dictionary<string, List<long>>();
                    foreach (GameServer g in listGS)
                        foreach (LoginHistoryEntry log in logins)
                            if (g.Id == log.serverId)
                            {
                                if (!loginsPerServer.ContainsKey(g.Name))
                                    loginsPerServer.Add(g.Name, new List<long>());

                                loginsPerServer[g.Name].Add(log.timestamp);
                            }
                    Dictionary<string, Dictionary<long, int>> loginsPerServerPerDay = new Dictionary<string, Dictionary<long, int>>();
                    foreach (GameServer g in listGS)
                    {
                        loginsPerServerPerDay.Add(g.Name, new Dictionary<long, int>());
                        for (int i = 0; i < 16; i++)
                        {
                            DateTime recent = DateTime.Now.AddDays(-i).Date;
                            long rtimestamp = new DateTimeOffset(recent).ToUnixTimeSeconds();
                            loginsPerServerPerDay[g.Name].Add(rtimestamp, 0);
                        }
                    }
                    foreach (KeyValuePair<string, List<long>> pair in loginsPerServer)
                    {
                        string serverName = pair.Key;
                        Dictionary<long, int> dateDic = loginsPerServerPerDay[serverName];
                        foreach (long t in pair.Value)
                        {
                            DateTime d = Helper.UnixTimeStampToDateTime(t).Date;
                            long ltimestamp = new DateTimeOffset(d).ToUnixTimeSeconds();
                            dateDic[ltimestamp] = dateDic[ltimestamp] + 1;
                        }
                    }
                    sb.Append("[");
                    foreach (string serverName in loginsPerServerPerDay.Keys)
                    {
                        sb.Append("{\"name\":\"" + serverName + "\",");
                        sb.Append("\"series\":[");
                        foreach (KeyValuePair<long, int> pair in loginsPerServerPerDay[serverName])
                        {
                            sb.Append("{\"value\":" + pair.Value);
                            sb.Append(",\"name\":" + pair.Key + "},");
                        }
                        if (loginsPerServerPerDay[serverName].Count > 0)
                            sb.Length--;
                        sb.Append("]},");
                    }
                    if (loginsPerServerPerDay.Keys.Count > 0)
                        sb.Length--;
                    sb.Append("]");
                    lastRLUpdateResult = sb.ToString();
                    Log.Print("HandleGetRecentLogins: recent logins refreshed");
                    return lastRLUpdateResult;
                }
            }
        }

        public static ulong GetPageViews()
        {
            ulong result = 1;
            lock (_sync)
            {
                SQLiteDataReader r = ExecuteSQL("SELECT * FROM " + table_stats + " WHERE name = 'pageViews'");
                r.Read();
                if (r.HasRows)
                {
                    string s = r.GetString(2);
                    result = ulong.Parse(s) + 1;
                    r.Close();
                }
                else
                {
                    r.Close();
                    ExecuteSimpleSQL("INSERT INTO stats (name, data) VALUES ('pageViews','0')");                    
                }
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE stats SET data=@data WHERE name='pageViews'";
                cmd.Parameters.AddWithValue("@data", result.ToString());
                cmd.ExecuteNonQuery();
            }
            return result;
        }

        public static void AddServerLoginHistory(int serverId, int userId)
        {
            lock (_sync)
            {
                string sql = "INSERT INTO " + table_history
                           + " (timestamp, server_id, user_id)"
                           + " VALUES (@timestamp, @server_id, @user_id)";
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@timestamp", DateTimeOffset.Now.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("@server_id", serverId);
                cmd.Parameters.AddWithValue("@user_id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public static void AddGameServer(GameServer g)
        {
            lock (_sync)
            {
                string sql = "INSERT INTO " + table_servers 
                           + " (public_key, name, ip, port_udp, port_tcp, status)"
                           + " VALUES (@public_key, @name, @ip, @port_udp, @port_tcp, @status)";
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@public_key", g.PublicKey);
                cmd.Parameters.AddWithValue("@name", g.Name);
                cmd.Parameters.AddWithValue("@ip", g.IP);
                cmd.Parameters.AddWithValue("@port_udp", g.PortUDP);
                cmd.Parameters.AddWithValue("@port_tcp", g.PortTCP);
                cmd.Parameters.AddWithValue("@status", g.Status);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "select last_insert_rowid()";
                long newId = (long)cmd.ExecuteScalar();
                g.Id = (int)newId;
                servers.Add(g);
            }
        }

        public static void AddPlayerProfile(PlayerProfile p)
        {
            lock (_sync)
            {
                string sql = "INSERT INTO " + table_profiles + " (public_key, name, meta_data) VALUES (@public_key, @name, @meta_data)";
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@public_key", p.PublicKey);
                cmd.Parameters.AddWithValue("@name", p.Name);
                cmd.Parameters.AddWithValue("@meta_data", p.MetaData);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "select last_insert_rowid()";
                long newId = (long)cmd.ExecuteScalar();
                p.Id = (int)newId;
                profiles.Add(p);
                needsUpdate = true;
            }
        }

        public static void UpdateGameServer(GameServer g)
        {
            lock (_sync)
            {
                for (int i = 0; i < servers.Count; i++)
                    if (servers[i].Id == g.Id)
                    {
                        servers[i] = g;
                        break;
                    }
            }
        }

        public static void UpdatePlayerProfile(PlayerProfile p)
        {
            lock (_sync)
            {
                for (int i = 0; i < profiles.Count; i++)
                    if (profiles[i].Id == p.Id)
                    {
                        profiles[i] = p;
                        break;
                    }
            }
        }

        public static void RemoveGameServer(GameServer g)
        {
            lock (_sync)
            {
                string sql = "DELETE FROM '" + table_servers + "' WHERE id=" + g.Id;
                ExecuteSimpleSQL(sql);
                for (int i = 0; i < servers.Count; i++)
                    if (servers[i].Id == g.Id)
                    {
                        servers.RemoveAt(i);
                        break;
                    }
                needsUpdate = true;
            }
        }

        public static void RemovePlayerProfile(PlayerProfile p)
        {
            lock (_sync)
            {
                string sql = "DELETE FROM '" + table_profiles + "' WHERE id=" + p.Id;
                ExecuteSimpleSQL(sql);
                for (int i = 0; i < profiles.Count; i++)
                    if (profiles[i].Id == p.Id)
                    {
                        profiles.RemoveAt(i);
                        break;
                    }
                needsUpdate = true;
            }
        }

        static void SaveGameServer(GameServer gs)
        {
            string sql = "SELECT * FROM " + table_servers + " WHERE id=" + gs.Id;
            SQLiteDataReader r = ExecuteSQL(sql);
            if (r.HasRows)
            {
                sql = "UPDATE " + table_servers + " SET ";
                sql += "public_key=@public_key,";
                sql += "name=@name,";
                sql += "ip=@ip,";
                sql += "port_udp=@port_udp,";
                sql += "port_tcp=@port_tcp,";
                sql += "status=@status";
                sql += " WHERE id=" + gs.Id;
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@public_key", gs.PublicKey);
                cmd.Parameters.AddWithValue("@name", gs.Name);
                cmd.Parameters.AddWithValue("@ip", gs.IP);
                cmd.Parameters.AddWithValue("@port_udp", gs.PortUDP);
                cmd.Parameters.AddWithValue("@port_tcp", gs.PortTCP);
                cmd.Parameters.AddWithValue("@status", gs.Status);
                cmd.ExecuteNonQuery();
                gs.Reset();
            }
        }

        private static void SavePlayerProfile(PlayerProfile p)
        {
            string sql = "SELECT * FROM " + table_profiles + " WHERE id=" + p.Id;
            SQLiteDataReader r = ExecuteSQL(sql);
            if (r.HasRows)
            {
                sql = "UPDATE " + table_profiles + " SET ";
                sql += "public_key=@public_key,";
                sql += "name=@name,";
                sql += "meta_data=@meta_data";
                sql += " WHERE id=" + p.Id;
                SQLiteCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@public_key", p.PublicKey);
                cmd.Parameters.AddWithValue("@name", p.Name);
                cmd.Parameters.AddWithValue("@meta_data", p.MetaData);
                cmd.ExecuteNonQuery();
                p.Reset();
            }
        }

        public static bool NeedsUpdate()
        {
            if (needsUpdate)
                return true;
            lock (_sync)
            {
                foreach (GameServer gs in servers)
                    if (gs.NeedsUpdate)
                        return true;
                foreach (PlayerProfile p in profiles)
                    if (p.NeedsUpdate)
                        return true;
            }
            return false;
        }

        public static void Update()
        {
            needsUpdate = false;
            lock (_sync)
            {
                bool foundPlayerUpdate = false;
                foreach (GameServer gs in servers)
                    if (gs.NeedsUpdate)
                        SaveGameServer(gs);
                foreach (PlayerProfile p in profiles)
                    if (p.NeedsUpdate)
                    {
                        SavePlayerProfile(p);
                        foundPlayerUpdate = true;
                    }
                if (foundPlayerUpdate)
                    lock (_sync)
                    {
                        playerProfileUpdateCounter++;
                    }
            }
        }

        public static uint GetPlayerProfileUpdateCounter()
        {
            lock (_sync)
            {
                return playerProfileUpdateCounter;
            }
        }

        public static void Reload(bool silent = true)
        {
            lock (_sync)
            {
                LoadAllServers(silent);
                LoadAllProfiles(silent);
            }
        }

        private static void ExecuteSimpleSQL(string sql)
        {
            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        private static SQLiteDataReader ExecuteSQL(string sql)
        {
            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteReader();
        }
    }
}

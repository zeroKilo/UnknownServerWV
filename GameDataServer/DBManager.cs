using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace GameDataServer
{
    public static class DBManager
    {
        private static string db_path = "";
        private static SQLiteConnection connection = null;
        private static List<PlayerProfile> profiles = new List<PlayerProfile>();
        private static List<GameServer> servers = new List<GameServer>();
        private static readonly object _sync = new object();

        private static string table_servers = "servers";
        private static string table_profiles = "profiles";

        private static bool needsUpdate = false;

        private static uint playerProfileUpdateCounter = 1;
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
            List<string> names = new List<string>(new string[] { table_servers, table_profiles });
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
                case "servers":
                    sql = "CREATE TABLE "+ table_servers + " (id INTEGER PRIMARY KEY,public_key TEXT NOT NULL," +
                          "name TEXT NOT NULL,ip TEXT NOT NULL,port_udp TEXT NOT NULL," +
                          "port_tcp TEXT NOT NULL,status TEXT NOT NULL)";
                    break;
                case "profiles":
                    sql = "CREATE TABLE " + table_profiles + " (id INTEGER PRIMARY KEY,public_key TEXT NOT NULL," +
                          "name TEXT NOT NULL, meta_data TEXT NOT NULL)";
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
            bool result = false;
            lock (_sync)
            {
                foreach (GameServer gs in servers)
                    if (gs.NeedsUpdate)
                    {
                        result = true;
                        break;
                    }
                if (!result)
                    foreach (PlayerProfile p in profiles)
                        if (p.NeedsUpdate)
                        {
                            result = true;
                            break;
                        }
            }
            return result;
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

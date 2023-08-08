using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NetDefines;

namespace Server
{
    public static class SpawnManager
    {
        public class SpawnRange
        {
            public int start;
            public int end;
            public int count;

            public SpawnRange(int s, int e, int c)
            {
                start = s;
                end = e;
                count = c;
            }

            public bool Contains(int x)
            {
                return x >= start && x <= end;
            }
        }

        public static List<SpawnGroupRemoveInfo> spawnGroupChanges = new List<SpawnGroupRemoveInfo>();
        public static List<DroppedItemInfo> droppedItems = new List<DroppedItemInfo>();
        public static List<ItemContainerInfo> itemContainers = new List<ItemContainerInfo>();
        public static List<SpawnRange> itemSpawnRanges = new List<SpawnRange>();
        public static int maxSpawnRange;
        private static Random rnd = new Random();

        public static void Reset()
        {
            spawnGroupChanges = new List<SpawnGroupRemoveInfo>();
            droppedItems = new List<DroppedItemInfo>();
            itemContainers = new List<ItemContainerInfo>();
            LoadSpawnRanges();
            Log.Print("RESET SpawnManager");
        }

        private static void LoadSpawnRanges()
        {
            string[] lines = File.ReadAllLines("spawn_table.txt");
            Array values = Enum.GetValues(typeof(Item));
            SpawnRange[] spawnRanges = new SpawnRange[values.Length - 1];
            int idx = 0;
            int pos = 0;
            foreach(string line in lines)
            {
                string s = line;
                if (s.Contains("//"))
                    s = line.Substring(0, line.IndexOf("//"));
                s = s.Trim();
                if (s == "")
                    continue;
                if (!s.Contains(','))
                    continue;
                string[] parts = s.Split(',');
                int size = Convert.ToInt32(parts[0].Trim());
                int count = Convert.ToInt32(parts[1].Trim());
                spawnRanges[idx++] = new SpawnRange(pos, pos + size - 1, count);
                pos += size;
            }
            maxSpawnRange = pos;
            itemSpawnRanges = new List<SpawnRange>(spawnRanges);
        }

        private static ItemSpawnInfo GetRandomItem()
        {
            int n = rnd.Next(0, maxSpawnRange);
            for (int i = 0; i < itemSpawnRanges.Count; i++)
                if (itemSpawnRanges[i].Contains(n))
                    return new ItemSpawnInfo((Item)i, (uint)itemSpawnRanges[i].count);
            return new ItemSpawnInfo(Item.UNDEFINED, 1);
        }

        public static void RegisterItemContainer(float[] pos, ItemContainerType type, string name, NetDefines.StateDefines.NetState_Inventory inventory)
        {
            ItemContainerInfo info = new ItemContainerInfo();
            info.location = pos;
            info.type = type;
            info.name = name;
            info.items.Clear();
            foreach (NetDefines.StateDefines.NetState_Inventory.State_Weapon weapon in inventory.weaponSlots)
                if (weapon != null && weapon.item != (uint)Item.UNDEFINED)
                {
                    info.items.Add(new ItemSpawnInfo((Item)weapon.item, 0));
                    foreach (uint u in weapon.modSlots)
                        if (u != (uint)Item.UNDEFINED)
                            info.items.Add(new ItemSpawnInfo((Item)u, 0));
                }
            foreach (ItemSpawnInfo item in inventory.bagContent)
                info.items.Add(item);
            foreach (ItemSpawnInfo item in inventory.clothContent)
                if (item.item != Item.UNDEFINED)
                    info.items.Add(item);
            info.removedIndicies = new List<int>();
            itemContainers.Add(info);
        }

        public static void RemoveDroppedItem(float[] pos)
        {
            for (int i = 0; i < droppedItems.Count; i++)
            {
                DroppedItemInfo info = droppedItems[i];
                if (info.location[0] == pos[0] &&
                    info.location[1] == pos[1] &&
                    info.location[2] == pos[2])
                {
                    droppedItems.RemoveAt(i);
                    return;
                }
            }
        }

        public static void AddItemContainerRemoval(float[] pos, int index)
        {
            foreach (ItemContainerInfo info in itemContainers)
                if (info.location[0] == pos[0] &&
                    info.location[1] == pos[1] &&
                    info.location[2] == pos[2])
                {
                    info.removedIndicies.Add(index);
                    return;
                }
        }

        public static void AddSpawnGroupRemoval(float[] pos, int index)
        {
            foreach (SpawnGroupRemoveInfo info in spawnGroupChanges)
                if (info.location[0] == pos[0] &&
                    info.location[1] == pos[1] &&
                    info.location[2] == pos[2])
                {
                    info.AddRemoval(index);
                    return;
                }
            SpawnGroupRemoveInfo info2 = new SpawnGroupRemoveInfo(pos);
            info2.AddRemoval(index);
            spawnGroupChanges.Add(info2);
        }

        public static void WriteDroppedItems(Stream s)
        {
            NetHelper.WriteU32(s, (uint)droppedItems.Count);
            foreach (DroppedItemInfo info in droppedItems)
            {
                foreach (float f in info.location)
                    NetHelper.WriteFloat(s, f);
                info.spawnInfo.Write(s);
            }
        }

        public static void WriteSpawnGroupRemovals(Stream s, float[] pos)
        {
            foreach (float f in pos)
                NetHelper.WriteFloat(s, f);
            foreach (SpawnGroupRemoveInfo info in spawnGroupChanges)
                if (info.location[0] == pos[0] &&
                    info.location[1] == pos[1] &&
                    info.location[2] == pos[2])
                {
                    NetHelper.WriteU32(s, (uint)info.indicies.Count);
                    foreach (int index in info.indicies)
                        NetHelper.WriteU32(s, (uint)index);
                    return;
                }
            NetHelper.WriteU32(s, 0);
        }

        public static void WriteItemContainers(Stream s)
        {
            NetHelper.WriteU32(s, (uint)itemContainers.Count);
            foreach (ItemContainerInfo info in itemContainers)
                info.Write(s);
        }

        public static List<ItemSpawnInfo> GetRandomSpawn(SpawnTierLevel tier, uint count)
        {
            List<ItemSpawnInfo> resultList = new List<ItemSpawnInfo>();
            for (int i = 0; i < count; i++)
                resultList.Add(GetRandomItem());
            return resultList;
        }
    }
}

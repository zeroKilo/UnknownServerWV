using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NetDefines;
using System.Xml.Linq;

namespace Server
{
    public static class SpawnManager
    {
        public class SpawnRange
        {
            public List<Item> items;
            public int start;
            public int end;

            public SpawnRange(List<Item> i, int s, int e)
            {
                items = i;
                start = s;
                end = e;
            }

            public bool Contains(int x)
            {
                return x >= start && x <= end;
            }

            public SpawnRange Copy()
            {
                return new SpawnRange(items, start, end);
            }
        }

        public static List<SpawnGroupRemoveInfo> spawnGroupChanges = new List<SpawnGroupRemoveInfo>();
        public static List<DroppedItemInfo> droppedItems = new List<DroppedItemInfo>();
        public static List<ItemContainerInfo> itemContainers = new List<ItemContainerInfo>();
        public static Dictionary<string, List<SpawnRange>> singleItemSpawnRanges = new Dictionary<string, List<SpawnRange>>();
        public static Dictionary<string, List<SpawnRange>> multiItemSpawnRanges = new Dictionary<string, List<SpawnRange>>();
        public static Dictionary<string, int> maxSingleSpawnRange;
        public static Dictionary<string, int> maxMultiSpawnRange;
        private static readonly Random rnd = new Random();
        private static XElement itemSettings;

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
            int pos;
            itemSettings = NetHelper.StringToJSON(Config.itemSettingsJson);
            XElement root = NetHelper.StringToJSON(File.ReadAllText("spawn_table.json"));
            maxSingleSpawnRange = new Dictionary<string, int>();
            maxMultiSpawnRange = new Dictionary<string, int>();
            singleItemSpawnRanges = new Dictionary<string, List<SpawnRange>>();
            multiItemSpawnRanges = new Dictionary<string, List<SpawnRange>>();
            XElement single = (XElement)root.Nodes().ElementAt(0);
            XElement multi = (XElement)root.Nodes().ElementAt(1);
            foreach (XElement mapNode in single.Nodes())
            {
                XNode[] entryNodeList = mapNode.Nodes().ToArray();
                string mapName = mapNode.Name.LocalName;
                pos = 0;
                List<SpawnRange> result = new List<SpawnRange>();
                foreach (XNode entry in entryNodeList)
                {
                    XNode[] data = ((XElement)entry).Nodes().ToArray();
                    int size = int.Parse(((XElement)data[0]).Value);
                    List<Item> items = new List<Item>() { (Item)int.Parse(((XElement)data[1]).Value) };
                    result.Add(new SpawnRange(items, pos, pos + size - 1));
                    pos += size;
                }
                singleItemSpawnRanges.Add(mapName, result);
                maxSingleSpawnRange.Add(mapName, pos);
            }
            foreach (XElement mapNode in multi.Nodes())
            {
                XNode[] entryNodeList = mapNode.Nodes().ToArray();
                string mapName = mapNode.Name.LocalName;
                pos = 0;
                List<SpawnRange> result = new List<SpawnRange>();
                foreach (XNode entry in entryNodeList)
                {
                    XNode[] data = ((XElement)entry).Nodes().ToArray();
                    int size = int.Parse(((XElement)data[0]).Value);
                    XNode[] subData = ((XElement)data[1]).Nodes().ToArray();
                    List<Item> items = new List<Item>();
                    foreach (XNode d in subData)
                        items.Add((Item)int.Parse(((XElement)d).Value));
                    result.Add(new SpawnRange(items, pos, pos + size - 1));
                    pos += size;
                }
                multiItemSpawnRanges.Add(mapName, result);
                maxMultiSpawnRange.Add(mapName, pos);
            }
        }

        private static ItemSpawnInfo GetRandomSingleItem(string map)
        {
            if (!singleItemSpawnRanges.ContainsKey(map))
                map = "_default";
            int n = rnd.Next(0, maxSingleSpawnRange[map]);
            for (int i = 0; i < singleItemSpawnRanges[map].Count; i++)
                if (singleItemSpawnRanges[map][i].Contains(n))
                {
                    int item = (int)singleItemSpawnRanges[map][i].items[0];
                    XElement itemData = GetItem(itemSettings, item);
                    if (itemData != null)
                    {
                        uint spawnCount = uint.Parse(GetPropertyValue(itemData, "itemCount"));
                        return new ItemSpawnInfo((Item)item, spawnCount);
                    }
                }
            return new ItemSpawnInfo(Item.UNDEFINED, 1);
        }

        private static List<ItemSpawnInfo> GetRandomMultipleItems(string map)
        {
            if (!multiItemSpawnRanges.ContainsKey(map))
                map = "_default";
            int n = rnd.Next(0, maxMultiSpawnRange[map]);
            for (int i = 0; i < multiItemSpawnRanges[map].Count; i++)
                if (multiItemSpawnRanges[map][i].Contains(n))
                {
                    List<ItemSpawnInfo> result = new List<ItemSpawnInfo>();
                    foreach (int item in multiItemSpawnRanges[map][i].items)
                    {
                        XElement itemData = GetItem(itemSettings, item);
                        if (itemData != null)
                        {
                            uint spawnCount = uint.Parse(GetPropertyValue(itemData, "itemCount"));
                            result.Add(new ItemSpawnInfo((Item)item, spawnCount));
                        }
                    }
                    return result;
                }
            return new List<ItemSpawnInfo>();
        }

        private static XElement GetItem(XElement root, int idx)
        {
            return (XElement)root.Nodes().ElementAt(idx);
        }

        private static string GetPropertyValue(XElement e, string propName)
        {
            foreach (XNode node2 in e.Nodes())
            {
                XElement x2 = (XElement)node2;
                if (x2.Name == propName)
                    return x2.Value;
            }
            return "";
        }

        public static void RegisterItemContainer(float[] pos, ItemContainerType type, string name, NetDefines.StateDefines.NetState_Inventory inventory)
        {
            ItemContainerInfo info = new ItemContainerInfo
            {
                location = pos,
                type = type,
                name = name
            };
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
                if (NetHelper.IsClose(info.location, pos))
                {
                    droppedItems.RemoveAt(i);
                    return;
                }
            }
        }

        public static void AddItemContainerRemoval(float[] pos, int index)
        {
            foreach (ItemContainerInfo info in itemContainers)
                if (NetHelper.IsClose(info.location, pos))
                {
                    info.removedIndicies.Add(index);
                    return;
                }
        }

        public static void AddSpawnGroupRemoval(float[] pos, int index)
        {
            foreach (SpawnGroupRemoveInfo info in spawnGroupChanges)
                if (NetHelper.IsClose(info.location, pos))
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
                if (NetHelper.IsClose(info.location, pos))
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

        public static List<ItemSpawnInfo> GetRandomSpawn(string map, uint count)
        {
            List<ItemSpawnInfo> resultList = new List<ItemSpawnInfo>();
            if (count == 1)
                resultList.Add(GetRandomSingleItem(map));
            else
            {
                List<ItemSpawnInfo> list = GetRandomMultipleItems(map);
                for (int i = 0; i < count; i++)
                    if (i < list.Count)
                        resultList.Add(list[i]);
                    else
                        resultList.Add(new ItemSpawnInfo(Item.UNDEFINED, 1));
            }
            return resultList;
        }
    }
}

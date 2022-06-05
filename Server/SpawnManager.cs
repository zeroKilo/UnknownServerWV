using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDefines;

namespace Server
{
    public static class SpawnManager
    {
        public static List<SpawnGroupRemoveInfo> spawnGroupChanges = new List<SpawnGroupRemoveInfo>();
        public static List<DroppedItemInfo> droppedItems = new List<DroppedItemInfo>();
        public static List<ItemContainerInfo> itemContainers = new List<ItemContainerInfo>();
        private static Random rnd = new Random();

        public static void Reset()
        {
            spawnGroupChanges = new List<SpawnGroupRemoveInfo>();
            droppedItems = new List<DroppedItemInfo>();
            itemContainers = new List<ItemContainerInfo>();
            Log.Print("RESET SpawnManager");
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
                    Log.Print("Removed index " + index);
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
            List<ItemSpawnInfo> chooseFrom = new List<ItemSpawnInfo>()
            {
                //new ItemSpawnInfo(Item.AK47, 10),
                //new ItemSpawnInfo(Item.M24, 10),
                //new ItemSpawnInfo(Item.M416, 10),
                //new ItemSpawnInfo(Item.M762, 10),
                //new ItemSpawnInfo(Item.SCAR_L, 10),
                //new ItemSpawnInfo(Item.QBZ, 10),
                new ItemSpawnInfo(Item.MK14, 10),
                new ItemSpawnInfo(Item.SKS, 10),
            };
            resultList.Add(chooseFrom[rnd.Next(0, chooseFrom.Count)]);
            resultList.Add(new ItemSpawnInfo(Item.EnergyDrink, 10));
            for (int i = 1; i < count - 1; i++)
                resultList.Add(new ItemSpawnInfo(Item.AmmoBoxNato762mm, 12));
            return resultList;
        }
    }
}

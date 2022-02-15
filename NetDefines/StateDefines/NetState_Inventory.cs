using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDefines.StateDefines
{
    public class NetState_Inventory
    {
        public class State_Weapon
        {
            public uint item;
            public uint[] modSlots;
        }

        public State_Weapon[] weaponSlots;
        public sbyte activeWeaponIndex;
        public bool hasBag;
        public List<ItemSpawnInfo> bagContent;
        public List<ItemSpawnInfo> clothContent;

        public static readonly uint initHash = 0x12345678;

        public NetState_Inventory()
        {
            weaponSlots = new State_Weapon[5];
            for (int i = 0; i < 5; i++)
                weaponSlots[i] = new State_Weapon();
            foreach (State_Weapon w in weaponSlots)
                w.modSlots = new uint[6];
            activeWeaponIndex = -1;
            bagContent = new List<ItemSpawnInfo>();
            clothContent = new List<ItemSpawnInfo>();
        }

        public void Read(Stream s)
        {
            foreach (State_Weapon w in weaponSlots)
            {
                w.item = NetHelper.ReadU32(s);
                for (int i = 0; i < w.modSlots.Length; i++)
                    w.modSlots[i] = NetHelper.ReadU32(s);
            }
            hasBag = s.ReadByte() == 1;
            activeWeaponIndex = (sbyte)s.ReadByte();
            uint count = NetHelper.ReadU32(s);
            bagContent = new List<ItemSpawnInfo>();
            for (int i = 0; i < count; i++)
                bagContent.Add(new ItemSpawnInfo(s));
            count = NetHelper.ReadU32(s);
            clothContent = new List<ItemSpawnInfo>();
            for (int i = 0; i < count; i++)
                clothContent.Add(new ItemSpawnInfo(s));
        }

        public void Write(Stream s)
        {
            foreach (State_Weapon w in weaponSlots)
            {
                NetHelper.WriteU32(s, w.item);
                for (int i = 0; i < w.modSlots.Length; i++)
                    NetHelper.WriteU32(s, w.modSlots[i]);
            }
            s.WriteByte((byte)(hasBag ? 1 : 0));
            s.WriteByte((byte)activeWeaponIndex);
            NetHelper.WriteU32(s, (uint)bagContent.Count);
            foreach (ItemSpawnInfo info in bagContent)
                info.Write(s);
            NetHelper.WriteU32(s, (uint)clothContent.Count);
            foreach (ItemSpawnInfo info in clothContent)
                info.Write(s);
        }

        public uint Hash()
        {
            uint result = initHash;
            foreach (State_Weapon w in weaponSlots)
            {
                result += w.item;
                for (int i = 0; i < w.modSlots.Length; i++)
                    result += w.modSlots[i];
            }
            result += (uint)(hasBag ? 1 : 0);
            result += (uint)activeWeaponIndex;
            return result;
        }
    }
}

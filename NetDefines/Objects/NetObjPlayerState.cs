using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using NetDefines.StateDefines;

namespace NetDefines
{
    public class NetObjPlayerState : NetObject
    {
        private readonly object _sync = new object();
        private uint playerID;
        private float[] position = new float[3];
        private float[] rotation = new float[4];
        private float[] input = new float[2];
        private byte blendTree = 0;
        private byte stance = 0;
        private byte vault = 0;
        private byte climb = 0;
        private sbyte equip = -1;
        private sbyte unequip = -1;
        private bool grounded = true;
        private bool jumping = false;
        private float[] aimPoint = new float[3];
        private float[] weaponPivot = new float[3];
        private bool rigOn = false;
        private bool isAnimating = false;
        private NetState_Inventory stateInventory = new NetState_Inventory();
        private string details = "";

        public NetObjPlayerState()
        {
            type = NetObjectType.PlayerState;
        }

        public override byte[] Create(bool withAccessKey)
        {
            MemoryStream m = new MemoryStream();
            if (withAccessKey)
                NetHelper.WriteU32(m, accessKey);
            else
                NetHelper.WriteU32(m, 0);
            NetHelper.WriteU32(m, ID);
            WriteUpdate(m);
            return m.ToArray();
        }

        public void UpdateTransform(float[] pos, float[] rot)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    position[i] = pos[i];
                for (int i = 0; i < 4; i++)
                    rotation[i] = rot[i];
            }
        }

        public void UpdateAnimator(float[] inp, byte bTree, byte st, byte vt, byte cl, sbyte eq, sbyte uneq, bool gr, bool jp)
        {
            lock (_sync)
            {
                for (int i = 0; i < 2; i++)
                    input[i] = inp[i];
                blendTree = bTree;
                stance = st;
                vault = vt;
                climb = cl;
                equip = eq;
                unequip = uneq;
                grounded = gr;
                jumping = jp;
            }
        }

        public void UpdateRig(bool enableRig, float[] aimPos, float[] pivotOffset, bool animating)
        {
            lock (_sync)
            {
                rigOn = enableRig;
                for (int i = 0; i < 3; i++)
                {
                    aimPoint[i] = aimPos[i];
                    weaponPivot[i] = pivotOffset[i];
                }
                isAnimating = animating;
            }
        }

        public override void ReadUpdate(Stream s)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    position[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < 4; i++)
                    rotation[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < 2; i++)
                    input[i] = NetHelper.ReadFloat(s);
                blendTree = (byte)s.ReadByte();
                stance = (byte)s.ReadByte();
                vault = (byte)s.ReadByte();
                climb = (byte)s.ReadByte();
                equip = (sbyte)s.ReadByte();
                unequip = (sbyte)s.ReadByte();
                byte flags = (byte)s.ReadByte();
                grounded = (flags & 1) != 0;
                jumping = (flags & 2) != 0;
                rigOn = (flags & 4) != 0;
                isAnimating = (flags & 8) != 0;
                for (int i = 0; i < 3; i++)
                    aimPoint[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < 3; i++)
                    weaponPivot[i] = NetHelper.ReadFloat(s);
                MakeDetails();
            }
        }

        public override void WriteUpdate(Stream s)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, position[i]);
                for (int i = 0; i < 4; i++)
                    NetHelper.WriteFloat(s, rotation[i]);
                for (int i = 0; i < 2; i++)
                    NetHelper.WriteFloat(s, input[i]);
                s.WriteByte(blendTree);
                s.WriteByte(stance);
                s.WriteByte(vault);
                s.WriteByte(climb);
                s.WriteByte((byte)equip);
                s.WriteByte((byte)unequip);
                byte flags = 0;
                if (grounded)
                    flags |= 1;
                if (jumping)
                    flags |= 2;
                if (rigOn)
                    flags |= 4;
                if (isAnimating)
                    flags |= 8;
                s.WriteByte(flags);
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, aimPoint[i]);
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, weaponPivot[i]);
            }
        }

        private void MakeDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\n Pos = (");
            foreach (float f in position)
                sb.Append(f + " ");
            sb.AppendLine(")");
            sb.Append(" Rot = (");
            foreach (float f in rotation)
                sb.Append(f + " ");
            sb.AppendLine(")");
            sb.Append(" Input = (");
            foreach (float f in input)
                sb.Append(f + " ");
            sb.AppendLine(")");
            sb.AppendLine(" BlendTree    = " + blendTree);
            sb.AppendLine(" Stance       = " + stance);
            sb.AppendLine(" Vault        = " + vault);
            sb.AppendLine(" Climb        = " + climb);
            sb.AppendLine(" Equip        = " + equip);
            sb.AppendLine(" Unequip      = " + unequip);
            sb.AppendLine(" Grounded     = " + grounded);
            sb.AppendLine(" Jumping      = " + jumping);
            sb.AppendLine(" Rig enabled  = " + rigOn);
            sb.AppendLine(" Is Animating = " + isAnimating);
            sb.Append(" AimPoint = (");
            foreach (float f in aimPoint)
                sb.Append(f + " ");
            sb.AppendLine(")");
            sb.Append(" WeaponPivot = (");
            foreach (float f in weaponPivot)
                sb.Append(f + " ");
            sb.AppendLine(")");
            details = sb.ToString();
        }

        public override string GetDetails()
        {
            return details;
        }

        public uint GetPlayerID()
        {
            uint result = 0;
            lock(_sync)
            {
                result = playerID;
            }
            return result;
        }

        public void SetPlayerID(uint ID)
        {
            lock(_sync)
            {
                playerID = ID;
            }
        }

        public float[] GetPosition()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = position[i];
            }
            return result;
        }

        public void SetPosition(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    position[i] = f[i];
            }
        }

        public float[] GetRotation()
        {
            float[] result = new float[4];
            lock (_sync)
            {
                for (int i = 0; i < 4; i++)
                    result[i] = rotation[i];
            }
            return result;
        }

        public void SetRotation(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 4; i++)
                    rotation[i] = f[i];
            }
        }

        public float[] GetInput()
        {
            float[] result = new float[2];
            lock (_sync)
            {
                result[0] = input[0];
                result[1] = input[1];
            }
            return result;
        }

        public void SetInput(float[] f)
        {
            lock (_sync)
            {
                input[0] = f[0];
                input[1] = f[1];
            }
        }

        public byte GetBlendTree()
        {
            byte result = 0;
            lock(_sync)
            {
                result = blendTree;
            }
            return result;
        }

        public void SetBlendTree(byte v)
        {
            lock (_sync)
            {
                blendTree = v;
            }
        }

        public byte GetStance()
        {
            byte result = 0;
            lock (_sync)
            {
                result = stance;
            }
            return result;
        }

        public void SetStance(byte v)
        {
            lock (_sync)
            {
                stance = v;
            }
        }

        public byte GetVault()
        {
            byte result = 0;
            lock (_sync)
            {
                result = vault;
            }
            return result;
        }

        public void SetVault(byte v)
        {
            lock (_sync)
            {
                vault = v;
            }
        }

        public byte GetClimb()
        {
            byte result = 0;
            lock (_sync)
            {
                result = climb;
            }
            return result;
        }

        public void SetClimb(byte v)
        {
            lock (_sync)
            {
                climb = v;
            }
        }

        public sbyte GetEquip()
        {
            sbyte result = -1;
            lock (_sync)
            {
                result = equip;
            }
            return result;
        }

        public void SetEquip(sbyte v)
        {
            lock (_sync)
            {
                equip = v;
            }
        }

        public sbyte GetUnEquip()
        {
            sbyte result = -1;
            lock (_sync)
            {
                result = unequip;
            }
            return result;
        }

        public void SetUnEquip(sbyte v)
        {
            lock (_sync)
            {
                unequip = v;
            }
        }

        public bool GetGrounded()
        {
            bool result = false;
            lock(_sync)
            {
                result = grounded;
            }
            return result;
        }

        public void SetGrounded(bool v)
        {
            lock (_sync)
            {
                grounded = v;
            }
        }

        public bool GetJumping()
        {
            bool result = false;
            lock (_sync)
            {
                result = jumping;
            }
            return result;
        }

        public void SetJumping(bool v)
        {
            lock (_sync)
            {
                jumping = v;
            }
        }

        public bool GetRigOn()
        {
            bool result = false;
            lock (_sync)
            {
                result = rigOn;
            }
            return result;
        }

        public void SetRigOn(bool v)
        {
            lock (_sync)
            {
                rigOn = v;
            }
        }

        public float[] GetAimPoint()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = aimPoint[i];
            }
            return result;
        }

        public void SetAimPoint(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    aimPoint[i] = f[i];
            }
        }

        public float[] GetWeaponPivot()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = weaponPivot[i];
            }
            return result;
        }

        public void SetWeaponPivot(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    weaponPivot[i] = f[i];
            }
        }

        public bool IsAnimating()
        {
            bool result = false;
            lock (_sync)
            {
                result = isAnimating;
            }
            return result;
        }

        public void SetAnimating(bool v)
        {
            lock (_sync)
            {
                isAnimating = v;
            }
        }

        public NetState_Inventory GetStateInventory()
        {
            NetState_Inventory result = null;
            lock(_sync)
            {
                result = stateInventory;
            }
            return result;
        }

        public void SetStateInventory(NetState_Inventory inventory)
        {
            lock (_sync)
            {
                stateInventory = inventory;
            }
        }
    }
}

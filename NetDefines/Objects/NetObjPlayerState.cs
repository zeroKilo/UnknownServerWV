using System.IO;
using System.Text;
using NetDefines.StateDefines;

namespace NetDefines
{
    public class NetObjPlayerState : NetObject
    {
        public uint playerID;
        public float[] position = new float[3];
        public float[] rotation = new float[4];
        public float[] input = new float[2];
        public byte blendTree = 0;
        public byte stance = 0;
        public byte vault = 0;
        public byte climb = 0;
        public sbyte equip = -1;
        public sbyte unequip = -1;
        public bool grounded = true;
        public bool jumping = false;
        public float[] aimPoint = new float[3];
        public float[] weaponPivot = new float[3];
        public bool rigOn = false;
        public bool isAnimating = false;
        public NetState_Inventory stateInventory = new NetState_Inventory();

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
            for (int i = 0; i < 3; i++)
                position[i] = pos[i];
            for (int i = 0; i < 4; i++)
                rotation[i] = rot[i];
        }

        public void UpdateAnimator(float[] inp, byte bTree, byte st, byte vt, byte cl, sbyte eq, sbyte uneq, bool gr, bool jp)
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

        public void UpdateRig(bool enableRig, float[] aimPos, float[] pivotOffset, bool animating)
        {
            rigOn = enableRig;
            for (int i = 0; i < 3; i++)
            {
                aimPoint[i] = aimPos[i];
                weaponPivot[i] = pivotOffset[i];
            }
            isAnimating = animating;
        }

        public override void ReadUpdate(Stream s)
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
        }

        public override void WriteUpdate(Stream s)
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

        public override string GetDetails()
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
            sb.Append("\n AimPoint = (");
            foreach (float f in aimPoint)
                sb.Append(f + " ");
            sb.AppendLine(")");
            sb.Append("\n WeaponPivot = (");
            foreach (float f in weaponPivot)
                sb.Append(f + " ");
            sb.AppendLine(")");
            return sb.ToString();
        }
    }
}

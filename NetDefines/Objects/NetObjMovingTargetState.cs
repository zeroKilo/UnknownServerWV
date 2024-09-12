using System.IO;
using System.Text;

namespace NetDefines.Objects
{
    public class NetObjMovingTargetState : NetObject
    {
        private string details = "";
        private float t;
        private float[] pos = new float[3];
        private readonly object _sync = new object();
        public NetObjMovingTargetState() 
        {
            type = NetObjectType.MovingTargetState;
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

        public override string GetDetails()
        {
            string result;
            lock (_sync)
            {
                result = details;
            }
            return result;
        }

        public override void ReadUpdate(Stream s)
        {
            lock (_sync)
            {
                t = NetHelper.ReadFloat(s);
            }
        }

        public override void WriteUpdate(Stream s)
        {
            lock (_sync)
            {
                NetHelper.WriteFloat(s, t);
                MakeDetails();
            }
        }
        private void MakeDetails()
        {
            details = " T = " + t;
        }

        public void RefreshDetails()
        {
            lock (_sync)
            {
                MakeDetails();
            }
        }

        public float GetT()
        {
            float result = 0;
            lock (_sync)
            {
                result = t;
            }
            return result;
        }

        public void SetT(float t)
        {
            lock (_sync)
            {
                this.t = t;
            }
        }

        public float[] GetPos()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = pos[i];
            }
            return result;
        }

        public void SetPos(float[] pos)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    this.pos[i] = pos[i];
            }
        }
    }
}

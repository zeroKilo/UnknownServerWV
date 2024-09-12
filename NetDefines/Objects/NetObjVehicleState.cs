using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetDefines
{
    public class NetObjVehicleState : NetObject
    {
        public enum VehiclePrefab
        {
            Buggy,
            Pickup1,
            Pickup2,
            MiradoOpen,
            MiradoClosed,
            Rony,
            MiniBus,
            Dacia,
            UAZ1,
            UAZ2,
            UAZ3,
            UAZ4,
            UAZ5,
            UAZ6,
        }

        public enum VehicleFlags
        {
            IS_IN_NEUTRAL = 0x01,
            PLAY_START_SOUND = 0x02,
            MOTOR_RUNNING = 0x04
        }

        public enum VehicleType
        {
            Car
        }

        public static Dictionary<VehiclePrefab, VehicleType> prefabTypeMap = new Dictionary<VehiclePrefab, VehicleType>
        {
            { VehiclePrefab.Buggy, VehicleType.Car },
            { VehiclePrefab.Pickup1, VehicleType.Car },
            { VehiclePrefab.Pickup2, VehicleType.Car },
            { VehiclePrefab.MiradoOpen, VehicleType.Car },
            { VehiclePrefab.MiradoClosed, VehicleType.Car },
            { VehiclePrefab.Rony, VehicleType.Car },
            { VehiclePrefab.MiniBus, VehicleType.Car },
            { VehiclePrefab.Dacia, VehicleType.Car },
            { VehiclePrefab.UAZ1, VehicleType.Car },
            { VehiclePrefab.UAZ2, VehicleType.Car },
            { VehiclePrefab.UAZ3, VehicleType.Car },
            { VehiclePrefab.UAZ4, VehicleType.Car },
            { VehiclePrefab.UAZ5, VehicleType.Car },
            { VehiclePrefab.UAZ6, VehicleType.Car },
        };

        public static readonly int MAX_SEAT_COUNT = 6;

        private VehiclePrefab vehiclePrefab;
        private VehicleType vehicleType;
        private float steer;
        private float[] position = new float[3];
        private float[] velocity = new float[3];
        private float[] rotation = new float[3];
        private uint[] seatPlayerIds = new uint[MAX_SEAT_COUNT];
        private byte wheelCount = 0;
        private byte seatCount = 0;
        private float[][] wheelRotations;
        private string details = "";
        private byte flags = 0;
        private readonly object _sync = new object();

        public NetObjVehicleState()
        {
            type = NetObjectType.VehicleState;
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
                vehiclePrefab = (VehiclePrefab)NetHelper.ReadU32(s);
                vehicleType = (VehicleType)NetHelper.ReadU32(s);
                steer = NetHelper.ReadFloat(s);
                for (int i = 0; i < 3; i++)
                    position[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < 3; i++)
                    velocity[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < 3; i++)
                    rotation[i] = NetHelper.ReadFloat(s);
                for (int i = 0; i < MAX_SEAT_COUNT; i++)
                    seatPlayerIds[i] = NetHelper.ReadU32(s);
                seatCount = (byte)s.ReadByte();
                wheelCount = (byte)s.ReadByte();
                wheelRotations = new float[wheelCount][];
                for (int i = 0; i < wheelCount; i++)
                {
                    wheelRotations[i] = new float[3];
                    for (int j = 0; j < 3; j++)
                        wheelRotations[i][j] = NetHelper.ReadFloat(s);
                }
                flags = (byte)s.ReadByte();
            }
        }

        public override void WriteUpdate(Stream s)
        {
            lock (_sync)
            {
                NetHelper.WriteU32(s, (uint)vehiclePrefab);
                NetHelper.WriteU32(s, (uint)vehicleType);
                NetHelper.WriteFloat(s, steer);
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, position[i]);
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, velocity[i]);
                for (int i = 0; i < 3; i++)
                    NetHelper.WriteFloat(s, rotation[i]);
                for (int i = 0; i < MAX_SEAT_COUNT; i++)
                    NetHelper.WriteU32(s, seatPlayerIds[i]);
                s.WriteByte(seatCount);
                s.WriteByte(wheelCount);
                for (int i = 0; i < wheelCount; i++)
                    for (int j = 0; j < 3; j++)
                        NetHelper.WriteFloat(s, wheelRotations[i][j]);
                s.WriteByte(flags);
                MakeDetails();
            }
        }

        private void MakeDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n Vehicle Prefab = " + vehiclePrefab);
            sb.AppendLine(" Vehicle Type = " + vehicleType);
            sb.AppendLine(" Steer = " + steer);
            sb.Append(" Pos = (");
            for (int i = 0; i < 3; i++)
                sb.Append(position[i] + " ");
            sb.AppendLine(")");
            sb.Append(" Vel = (");
            for (int i = 0; i < 3; i++)
                sb.Append(velocity[i] + " ");
            sb.AppendLine(")");
            sb.Append(" Rot = (");
            for (int i = 0; i < 3; i++)
                sb.Append(rotation[i] + " ");
            sb.AppendLine(")");
            sb.Append(" Seat Player IDs = (");
            for (int i = 0; i < MAX_SEAT_COUNT; i++)
                sb.Append(seatPlayerIds[i].ToString("X8") + " ");
            sb.AppendLine(")");
            sb.AppendLine(" Seat Count = " + seatCount);
            sb.AppendLine(" Wheel Count = " + wheelCount);
            for (int i = 0; i < wheelCount; i++)
            {
                sb.Append(" Wheel " + i + " = (");
                for (int j = 0; j < 3; j++)
                    sb.Append(wheelRotations[i][j] + " ");
                sb.AppendLine(")");
            }
            sb.Append(" Flags =");
            VehicleFlags[] fs = (VehicleFlags[])Enum.GetValues(typeof(VehicleFlags));
            foreach (VehicleFlags f in fs)
                if ((flags & (byte)f) != 0)
                    sb.Append(" " + f);
            sb.AppendLine();
            details = sb.ToString();
        }

        public void RefreshDetails()
        {
            lock(_sync)
            {
                MakeDetails();
            }
        }

        public byte GetWheelCount()
        {
            byte result = 0;
            lock (_sync)
            {
                result = wheelCount;
            }
            return result;
        }

        public void SetWheelCount(byte count)
        {
            lock (_sync)
            {
                wheelCount = count;
            }
        }

        public byte GetSeatCount()
        {
            byte result;
            lock (_sync)
            {
                result = seatCount;
            }
            return result;
        }

        public void SetSeatCount(byte count)
        {
            lock (_sync)
            {
                seatCount = count;
            }
        }

        public void SetVehiclePrefab(VehiclePrefab prefab)
        {
            lock(_sync)
            {
                vehiclePrefab = prefab;
            }
        }

        public VehiclePrefab GetVehiclePrefab()
        {
            VehiclePrefab result;
            lock (_sync)
            {
                result = vehiclePrefab;
            }
            return result;
        }

        public void SetVehicleType(VehicleType type)
        {
            lock (_sync)
            {
                vehicleType = type;
            }
        }

        public VehicleType GetVehicleType()
        {
            VehicleType result;
            lock (_sync)
            {
                result = vehicleType;
            }
            return result;
        }

        public uint GetSeatPlayerID(int idx)
        {
            uint result = 0;
            lock (_sync)
            {
                if(idx >= 0 && idx < seatCount)
                    result = seatPlayerIds[idx];
            }
            return result;
        }

        public void SetSeatPlayerID(int idx, uint id)
        {
            lock (_sync)
            {
                if (idx >= 0 && idx < seatCount)
                    seatPlayerIds[idx] = id;
            }
        }

        public float GetSteering()
        {
            float result = 0;
            lock(_sync)
            {
                result = steer;
            }
            return result;
        }

        public void SetSteering(float f)
        {
            lock(_sync)
            {
                steer = f;
            }
        }

        public float[] GetPosition()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for(int i = 0; i < 3; i++)
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
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = rotation[i];
            }
            return result;
        }

        public void SetRotation(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    rotation[i] = f[i];
            }
        }

        public float[] GetVelocity()
        {
            float[] result = new float[3];
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    result[i] = velocity[i];
            }
            return result;
        }

        public void SetVelocity(float[] f)
        {
            lock (_sync)
            {
                for (int i = 0; i < 3; i++)
                    velocity[i] = f[i];
            }
        }

        public float[][] GetWheelRotations()
        {
            float[][] result;
            lock (_sync)
            {
                result = new float[wheelCount][];
                for(int i = 0; i < wheelCount; i++)
                {
                    result[i] = new float[3];
                    for (int j = 0; j < 3; j++)
                        result[i][j] = wheelRotations[i][j];
                }
            }
            return result;
        }

        public void SetWheelRotations(float[][] f)
        {
            lock (_sync)
            {
                wheelRotations = new float[wheelCount][];
                for (int i = 0; i < wheelCount; i++)
                {
                    wheelRotations[i] = new float[3];
                    for (int j = 0; j < 3; j++)
                        wheelRotations[i][j] = f[i][j];
                }
            }
        }

        public byte GetFlags()
        {
            byte result;
            lock (_sync)
            {
                result = flags;
            }
            return result;
        }

        public void SetFlags(byte b)
        {
            lock (_sync)
            {
                flags = b;
            }
        }
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDefines;

namespace Server
{
    public static class BlueZoneManager
    {
        //public static List<BlueZoneStateStep> stateSteps = new List<BlueZoneStateStep>()
        //{
        //    new BlueZoneStateStep(BlueZoneState.Waiting,    456.47f,    296.71f,    300,    0.4f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
        //    new BlueZoneStateStep(BlueZoneState.Shrinking,  456.47f,    296.71f,    720,    0.4f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
        //
        //    new BlueZoneStateStep(BlueZoneState.Waiting,    296.71f,    148.35f,    200,    0.6f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
        //    new BlueZoneStateStep(BlueZoneState.Shrinking,  296.71f,    148.35f,    340,    0.6f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
        //
        //};

        public static List<BlueZoneStateStep> stateSteps = new List<BlueZoneStateStep>()
        {
            new BlueZoneStateStep(BlueZoneState.Waiting,    4564.7f,    2967.1f,    300,    0.4f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  4564.7f,    2967.1f,    720,    0.4f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    2967.1f,    1483.5f,    200,    0.6f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  2967.1f,    1483.5f,    340,    0.6f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    1483.5f,    741.8f,     150,    0.8f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  1483.5f,    741.8f,     240,    0.8f,   new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    741.8f,     370.9f,     120,    1f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  741.8f,     370.9f,     180,    1f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    370.9f,     185.4f,     120,    3f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  370.9f,     185.4f,     160,    3f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    185.4f,     92.7f,      90,     5f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  185.4f,     92.7f,      120,    5f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    92.7f,      46.4f,      60,     5f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  92.7f,      46.4f,      90,     5f,     new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),

            new BlueZoneStateStep(BlueZoneState.Waiting,    46.4f,      0.1f,       180,    11f,    new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
            new BlueZoneStateStep(BlueZoneState.Shrinking,  46.4f,      0.1f,       15,     11f,    new float[]{ 2048f, -2048f },   new float[]{ 2048f, -2048f }),
        };

        public static int currentStep = 0;
        public static Stopwatch sw = new Stopwatch();

        private static bool sendCurrentState = false;
        private static Random rnd = new Random();

        public static void Reset()
        {
            sw = new Stopwatch();
            sw.Start();
            currentStep = 0;
            sendCurrentState = true;
            for(int i = 0; i < stateSteps.Count / 2; i++)
            {
                float angle = (float)rnd.NextDouble() * 360f;
                float r1 = stateSteps[i * 2].radius;
                float r2 = stateSteps[i * 2 + 1].targetRadius;
                float r3 = r1 - r2;
                float dis = (float)rnd.NextDouble() * r3;
                float[] newCenter = new float[2];
                float[] oldCenter = stateSteps[i * 2].center;
                float tau = 3.1415f / 180f;
                newCenter[0] = oldCenter[0] + (float)Math.Sin(angle * tau) * dis;
                newCenter[1] = oldCenter[1] + (float)Math.Cos(angle * tau) * dis;
                stateSteps[i * 2].center = oldCenter;
                stateSteps[i * 2].nextCenter = newCenter;
                stateSteps[i * 2 + 1].center = oldCenter;
                stateSteps[i * 2 + 1].nextCenter = newCenter;
                if (i * 2 + 2 < stateSteps.Count)
                    stateSteps[i * 2 + 2].center = newCenter;
            }
        }

        public static void Update()
        {
            if(currentStep < stateSteps.Count)
            {
                if (sendCurrentState)
                {
                    sendCurrentState = false;
                    MemoryStream m = new MemoryStream();
                    stateSteps[currentStep].Write(m);
                    Backend.BroadcastCommand((uint)BackendCommand.UpdateBlueZoneReq, m.ToArray());
                }
                BlueZoneStateStep step = stateSteps[currentStep];
                if(sw.ElapsedMilliseconds / 1000f > step.time)
                {
                    sw.Restart();
                    sendCurrentState = true;
                    currentStep++;
                }
            }
        }

    }
}

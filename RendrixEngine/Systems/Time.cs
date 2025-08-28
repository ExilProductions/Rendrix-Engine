using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine
{
    public class Time
    {
        public static float DeltaTime { get; internal set; }

        public static float TimeSinceStart { get; internal set; }
    }
}
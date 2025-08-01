using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Systems
{
    public class Time
    {
        /// <summary>
        /// Do not set this value directly.
        /// </summary>
        public static float DeltaTime { get; set; }
        /// <summary>
        /// Do not set this value directly.
        /// </summary>
        public static float TimeSinceStart { get; set; }
    }
}

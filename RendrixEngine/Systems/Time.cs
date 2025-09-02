namespace RendrixEngine
{
    public static class Time
    {
        // Time between the last frame and this frame (scaled by TimeScale)
        public static float DeltaTime { get; internal set; }

        // Raw delta time unaffected by TimeScale
        public static float UnscaledDeltaTime { get; internal set; }

        // Total time since engine started (scaled by TimeScale)
        public static float TimeSinceStart { get; internal set; }

        // Total real time since engine started (unscaled)
        public static float RealtimeSinceStartup { get; internal set; }

        // Time scale factor
        public static float TimeScale { get; set; } = 1.0f;

        // Number of frames since engine start
        public static int FrameCount { get; internal set; }
    }
}

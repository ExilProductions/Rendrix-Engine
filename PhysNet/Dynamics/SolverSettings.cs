namespace PhysNet.Dynamics
{
    public enum CombineMode
    {
        Max,
        Min,
        Multiply,
        Average
    }

    public sealed class SolverSettings
    {
        public int Iterations { get; set; } = 10;
        public float PenetrationSlop { get; set; } = 0.01f; // meters
        public float Baumgarte { get; set; } = 0.2f; // positional bias factor
        public CombineMode FrictionCombine { get; set; } = CombineMode.Multiply;
        public CombineMode RestitutionCombine { get; set; } = CombineMode.Max;
    }
}

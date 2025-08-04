namespace RendrixEngine.Mathematics
{
    public struct Vector2D
    {
        public float X { get; }
        public float Y { get; }

        public static Vector2D Zero => new Vector2D(0, 0);

        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
        public static Vector2D operator *(float s, Vector2D v) => new Vector2D(s * v.X, s * v.Y);
        public static Vector2D operator *(Vector2D v, float s) => s * v;
    }
}

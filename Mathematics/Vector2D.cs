namespace Ascii3DRenderer.Mathematics
{
    /// <summary>
    /// Represents a 2D vector with x, y components.
    /// </summary>
    public struct Vector2D
    {
        public float X { get; }
        public float Y { get; }

        public Vector2D(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
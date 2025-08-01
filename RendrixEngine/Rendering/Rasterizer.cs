
using System;
using System.Collections.Generic;
using RendrixEngine.Components;
using RendrixEngine.Mathematics;

namespace RendrixEngine.Rendering
{
    public class Rasterizer
    {
        private readonly char[,] screenBuffer;
        private readonly float[,] zBuffer;
        public int Width { get; }
        public int Height { get; }
        private readonly string asciiChars;
        private const float FalloffConstant = 0.1f;

        public Rasterizer(int width, int height, string asciiChars)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive.");
            if (string.IsNullOrEmpty(asciiChars))
                throw new ArgumentException("ASCII character set cannot be empty.");

            Width = width;
            Height = height;
            this.asciiChars = asciiChars;
            screenBuffer = new char[height, width];
            zBuffer = new float[height, width];
            Clear();
        }

        public void Clear()
        {
            for (int j = 0; j < Height; j++)
                for (int i = 0; i < Width; i++)
                {
                    screenBuffer[j, i] = ' ';
                    zBuffer[j, i] = float.MaxValue;
                }
        }

        public void RasterizeTriangle(Vector2D p0, Vector2D p1, Vector2D p2, float z0, float z1, float z2, char asciiChar)
        {
            float minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
            float maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
            float minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
            float maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));

            int startI = Math.Max(0, (int)Math.Floor(minX));
            int endI = Math.Min(Width - 1, (int)Math.Ceiling(maxX));
            int startJ = Math.Max(0, (int)Math.Floor(minY));
            int endJ = Math.Min(Height - 1, (int)Math.Ceiling(maxY));

            for (int j = startJ; j <= endJ; j++)
            {
                for (int i = startI; i <= endI; i++)
                {
                    Vector2D p = new Vector2D(i + 0.5f, j + 0.5f);
                    float area = (p1.Y - p2.Y) * (p0.X - p2.X) + (p2.X - p1.X) * (p0.Y - p2.Y);
                    if (Math.Abs(area) < 1e-6f) continue;

                    float u = ((p1.Y - p2.Y) * (p.X - p2.X) + (p2.X - p1.X) * (p.Y - p2.Y)) / area;
                    float v = ((p2.Y - p0.Y) * (p.X - p2.X) + (p0.X - p2.X) * (p.Y - p2.Y)) / area;
                    float w = 1 - u - v;

                    if (u >= 0 && v >= 0 && w >= 0)
                    {
                        float z = u * z0 + v * z1 + w * z2;
                        if (z < zBuffer[j, i])
                        {
                            zBuffer[j, i] = z;
                            screenBuffer[j, i] = asciiChar;
                        }
                    }
                }
            }
        }

        public void RasterizeTriangleWithLighting(
            Vector2D p0, Vector2D p1, Vector2D p2,
            float z0, float z1, float z2,
            Vector3D worldPos0, Vector3D worldPos1, Vector3D worldPos2,
            Vector3D normal0, Vector3D normal1, Vector3D normal2,
            List<Light> lights, float ambientStrength, string asciiChars)
        {
            float minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
            float maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
            float minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
            float maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));

            int startI = Math.Max(0, (int)Math.Floor(minX));
            int endI = Math.Min(Width - 1, (int)Math.Ceiling(maxX));
            int startJ = Math.Max(0, (int)Math.Floor(minY));
            int endJ = Math.Min(Height - 1, (int)Math.Ceiling(maxY));

            for (int j = startJ; j <= endJ; j++)
            {
                for (int i = startI; i <= endI; i++)
                {
                    Vector2D p = new Vector2D(i + 0.5f, j + 0.5f);
                    float area = (p1.Y - p2.Y) * (p0.X - p2.X) + (p2.X - p1.X) * (p0.Y - p2.Y);
                    if (Math.Abs(area) < 1e-6f) continue;

                    float u = ((p1.Y - p2.Y) * (p.X - p2.X) + (p2.X - p1.X) * (p.Y - p2.Y)) / area;
                    float v = ((p2.Y - p0.Y) * (p.X - p2.X) + (p0.X - p2.X) * (p.Y - p2.Y)) / area;
                    float w = 1 - u - v;

                    if (u >= 0 && v >= 0 && w >= 0)
                    {
                        float z = u * z0 + v * z1 + w * z2;
                        if (z < zBuffer[j, i])
                        {
                            Vector3D worldPos = worldPos0 * u + worldPos1 * v + worldPos2 * w;
                            Vector3D normal = (normal0 * u + normal1 * v + normal2 * w).Normalized;
                            float brightness = CalculateLighting(worldPos, normal, lights, ambientStrength);
                            brightness = Math.Clamp(brightness, 0, 1);

                            char asciiChar = asciiChars[(int)(brightness * (asciiChars.Length - 1))];

                            zBuffer[j, i] = z;
                            screenBuffer[j, i] = asciiChar;
                        }
                    }
                }
            }
        }

        private float CalculateLighting(Vector3D worldPos, Vector3D normal, List<Light> lights, float ambientStrength)
        {
            float brightness = ambientStrength;

            foreach (var light in lights)
            {
                float diffuse = 0;
                if (light.Type == LightType.Directional)
                {
                    diffuse = Math.Max(0, Vector3D.Dot(normal, light.Direction)) * light.Intensity;
                }
                else
                {
                    Vector3D lightDir = (light.Transform.Position - worldPos);
                    float distance = lightDir.Length;

                    if (distance <= light.Range)
                    {
                        lightDir = lightDir.Normalized;
                        float falloff = light.Intensity / (distance * distance + FalloffConstant);
                        diffuse = Math.Max(0, Vector3D.Dot(normal, lightDir)) * falloff;
                    }
                }
                brightness += diffuse;
            }

            return brightness;
        }

        public string GetFrame()
        {
            var sb = new System.Text.StringBuilder();
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                    sb.Append(screenBuffer[j, i]);
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}

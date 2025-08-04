using System;
using System.Collections.Generic;
using RendrixEngine.Components;
using RendrixEngine.Mathematics;
using RendrixEngine.Models;

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
        private float previousAverageBrightness = 0f;
        private float indirectFactor;

        public Rasterizer(int width, int height, string asciiChars, float indirectFactor = 0.2f)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive.");
            if (string.IsNullOrEmpty(asciiChars))
                throw new ArgumentException("ASCII character set cannot be empty.");

            Width = width;
            Height = height;
            this.asciiChars = asciiChars;
            this.indirectFactor = indirectFactor;
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
            Vector2D uv0, Vector2D uv1, Vector2D uv2,
            Texture? texture,
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

                    float u_bary = ((p1.Y - p2.Y) * (p.X - p2.X) + (p2.X - p1.X) * (p.Y - p2.Y)) / area;
                    float v_bary = ((p2.Y - p0.Y) * (p.X - p2.X) + (p0.X - p2.X) * (p.Y - p2.Y)) / area;
                    float w_bary = 1 - u_bary - v_bary;

                    if (u_bary >= 0 && v_bary >= 0 && w_bary >= 0)
                    {
                        float z = u_bary * z0 + v_bary * z1 + w_bary * z2;
                        if (z < zBuffer[j, i])
                        {
                            Vector3D worldPos = worldPos0 * u_bary + worldPos1 * v_bary + worldPos2 * w_bary;
                            Vector3D normal = (normal0 * u_bary + normal1 * v_bary + normal2 * w_bary).Normalized;
                            float lightingBrightness = CalculateLighting(worldPos, normal, lights, ambientStrength);

                            float finalBrightness;
                            if (texture != null)
                            {
                                var interpolatedUV = uv0 * u_bary + uv1 * v_bary + uv2 * w_bary;
                                float u_coord = Math.Clamp(interpolatedUV.X, 0.0f, 1.0f);
                                float v_coord = Math.Clamp(interpolatedUV.Y, 0.0f, 1.0f);

                                var texColor = texture.GetPixel((int)(u_coord * (texture.Width - 1)), (int)(v_coord * (texture.Height - 1)));
                                float textureBrightness = (texColor.R / 255f * 0.299f) + (texColor.G / 255f * 0.587f) + (texColor.B / 255f * 0.114f);
                                finalBrightness = textureBrightness * lightingBrightness;
                            }
                            else
                            {
                                finalBrightness = lightingBrightness;
                            }

                            finalBrightness = Math.Clamp(finalBrightness, 0, 1);
                            char asciiChar = asciiChars[(int)(finalBrightness * (asciiChars.Length - 1))];

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

            float indirectLighting = indirectFactor * previousAverageBrightness;
            brightness += indirectLighting;

            return brightness;
        }

        public string GetFrame()
        {
            var sb = new System.Text.StringBuilder();
            float totalBrightness = 0;
            int pixelCount = Width * Height;

            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    char c = screenBuffer[j, i];
                    sb.Append(c);
                    if (c != ' ')
                    {
                        int index = asciiChars.IndexOf(c);
                        if (index != -1)
                        {
                            totalBrightness += (float)index / (asciiChars.Length - 1);
                        }
                    }
                }
                sb.Append('\n');
            }

            previousAverageBrightness = totalBrightness / pixelCount;
            return sb.ToString();
        }
    }
}
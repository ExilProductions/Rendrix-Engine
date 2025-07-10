
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Ascii3DRenderer.Mathematics;
using Ascii3DRenderer.Models;

namespace Ascii3DRenderer.Rendering
{
    /// <summary>
    /// Main renderer class that handles both rendering pipeline and rasterization with per-pixel lighting.
    /// </summary>
    public class Renderer
    {
        private readonly Camera camera;
        private readonly string asciiChars;
        private readonly float ambientStrength;
        private readonly char[,] screenBuffer;
        private readonly float[,] zBuffer;
        private const float FalloffConstant = 0.1f; // Prevents division by zero in inverse-square law

        public int Width { get; }
        public int Height { get; }

        public Renderer(int screenWidth, int screenHeight, Camera camera, string asciiChars, float ambientStrength)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                throw new ArgumentException("Width and height must be positive.");
            if (string.IsNullOrEmpty(asciiChars))
                throw new ArgumentException("ASCII character set cannot be empty.");
            if (ambientStrength < 0 || ambientStrength > 1)
                throw new ArgumentOutOfRangeException(nameof(ambientStrength), "Ambient strength must be between 0 and 1.");

            Width = screenWidth;
            Height = screenHeight;
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
            this.asciiChars = asciiChars;
            this.ambientStrength = ambientStrength;

            screenBuffer = new char[Height, Width];
            zBuffer = new float[Height, Width];
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

        public void Render(SceneNode rootNode)
        {
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode));

            // Collect all lights in the scene
            var lights = new List<Light>();
            CollectLights(rootNode, lights);

            Clear();
            RenderNode(rootNode, lights);
        }

        private void CollectLights(SceneNode node, List<Light> lights)
        {
            if (node.Light != null)
            {
                lights.Add(node.Light);
            }
            foreach (var child in node.Children)
            {
                CollectLights(child, lights);
            }
        }

        private void RenderNode(SceneNode node, List<Light> lights)
        {
            if (node.Mesh != null)
            {
                Matrix4x4 modelView = node.Transform.WorldMatrix * camera.ViewMatrix;

                foreach (var tri in node.Mesh.Triangles)
                {
                    // Transform vertices to camera space
                    Vector3D v0 = modelView.Transform(node.Mesh.Vertices[tri[0]]);
                    Vector3D v1 = modelView.Transform(node.Mesh.Vertices[tri[1]]);
                    Vector3D v2 = modelView.Transform(node.Mesh.Vertices[tri[2]]);

                    // Backface culling
                    Vector3D normalCamera = Vector3D.Cross(v1 - v0, v2 - v0).Normalized;
                    if (Vector3D.Dot(normalCamera, v0) > 0)
                        continue;

                    // Transform vertices to world space for lighting
                    Vector3D v0World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[0]]);
                    Vector3D v1World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[1]]);
                    Vector3D v2World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[2]]);

                    // Calculate world space normal for this triangle
                    Vector3D normalWorld = Vector3D.Cross(v1World - v0World, v2World - v0World).Normalized;

                    // Project to screen space
                    Vector2D p0 = Project(v0);
                    Vector2D p1 = Project(v1);
                    Vector2D p2 = Project(v2);

                    // Rasterize with per-pixel lighting
                    RasterizeTriangleWithLighting(
                        p0, p1, p2,
                        v0.Z, v1.Z, v2.Z,
                        v0World, v1World, v2World,
                        normalWorld, normalWorld, normalWorld, // Using same normal for all vertices (flat shading)
                        lights);
                }
            }

            // Recursively render children
            foreach (var child in node.Children)
            {
                RenderNode(child, lights);
            }
        }

        private Vector2D Project(Vector3D v)
        {
            Vector3D vProj = camera.ProjectionMatrix.Transform(v);
            if (vProj.Z <= 0) return new Vector2D(-1, -1); // Behind camera
            return new Vector2D(
                (vProj.X * 0.5f + 0.5f) * Width,
                (1 - (vProj.Y * 0.5f + 0.5f)) * Height
            );
        }

        private void RasterizeTriangleWithLighting(
            Vector2D p0, Vector2D p1, Vector2D p2,
            float z0, float z1, float z2,
            Vector3D worldPos0, Vector3D worldPos1, Vector3D worldPos2,
            Vector3D normal0, Vector3D normal1, Vector3D normal2,
            List<Light> lights)
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
                            // Interpolate world position and normal for this pixel
                            Vector3D worldPos = worldPos0 * u + worldPos1 * v + worldPos2 * w;
                            Vector3D normal = (normal0 * u + normal1 * v + normal2 * w).Normalized;

                            // Calculate per-pixel lighting
                            float brightness = CalculateLighting(worldPos, normal, lights);
                            brightness = Math.Clamp(brightness, 0, 1);

                            char asciiChar = asciiChars[(int)(brightness * (asciiChars.Length - 1))];

                            zBuffer[j, i] = z;
                            screenBuffer[j, i] = asciiChar;
                        }
                    }
                }
            }
        }

        private float CalculateLighting(Vector3D worldPos, Vector3D normal, List<Light> lights)
        {
            float brightness = ambientStrength;

            foreach (var light in lights)
            {
                float diffuse = 0;
                if (light.Type == Light.LightType.Directional)
                {
                    diffuse = Math.Max(0, Vector3D.Dot(normal, light.Direction)) * light.Intensity;
                }
                else // Point light
                {
                    Vector3D lightDir = (light.Position - worldPos);
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
            var sb = new StringBuilder();
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

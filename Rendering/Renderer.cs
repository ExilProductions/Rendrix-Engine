using System;
using System.Collections.Generic;
using System.Numerics;
using Ascii3DRenderer.Mathematics;
using Ascii3DRenderer.Models;

namespace Ascii3DRenderer.Rendering
{
    /// <summary>
    /// Main renderer class that orchestrates the rendering pipeline with grayscale lighting, including point light range and falloff.
    /// </summary>
    public class Renderer
    {
        private readonly Rasterizer rasterizer;
        private readonly Camera camera;
        private readonly string asciiChars;
        private readonly float ambientStrength;
        private const float FalloffConstant = 0.1f; // Prevents division by zero in inverse-square law

        public Renderer(int screenWidth, int screenHeight, Camera camera, string asciiChars, float ambientStrength)
        {
            if (ambientStrength < 0 || ambientStrength > 1)
                throw new ArgumentOutOfRangeException(nameof(ambientStrength), "Ambient strength must be between 0 and 1.");

            this.rasterizer = new Rasterizer(screenWidth, screenHeight, asciiChars);
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
            this.asciiChars = asciiChars;
            this.ambientStrength = ambientStrength;
        }

        public void Render(SceneNode rootNode)
        {
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode));

            // Collect all lights in the scene
            var lights = new List<Light>();
            CollectLights(rootNode, lights);

            rasterizer.Clear();
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

                    // Compute normal in world space for lighting
                    Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(node.Transform.Rotation);
                    Vector3D v0World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[0]]);
                    Vector3D v1World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[1]]);
                    Vector3D v2World = node.Transform.WorldMatrix.Transform(node.Mesh.Vertices[tri[2]]);
                    Vector3D normalWorld = Vector3D.Cross(v1World - v0World, v2World - v0World).Normalized;

                    float brightness = ambientStrength;
                    foreach (var light in lights)
                    {
                        float diffuse = 0;
                        if (light.Type == Light.LightType.Directional)
                        {
                            // Directional light calculation remains the same
                            diffuse = Math.Max(0, Vector3D.Dot(normalWorld, light.Direction)) * light.Intensity;
                        }
                        else // Point light
                        {
                            // The light's position is already in world space
                            Vector3D lightWorldPosition = light.Position; // Use light.Position directly

                            // Calculate the direction from the vertex (v0World) to the light
                            Vector3D lightDir = (lightWorldPosition - v0World);
                            float distance = lightDir.Length;

                            if (distance <= light.Range)
                            {
                                lightDir = lightDir.Normalized;
                                // Apply inverse-square falloff
                                float falloff = light.Intensity / (distance * distance + FalloffConstant);
                                diffuse = Math.Max(0, Vector3D.Dot(normalWorld, lightDir)) * falloff;
                            }
                        }
                        brightness += diffuse;
                    }
                    brightness = Math.Clamp(brightness, 0, 1);
                    char asciiChar = asciiChars[(int)(brightness * (asciiChars.Length - 1))];

                    // Project to screen space
                    Vector2D p0 = Project(v0);
                    Vector2D p1 = Project(v1);
                    Vector2D p2 = Project(v2);

                    // Rasterize
                    rasterizer.RasterizeTriangle(p0, p1, p2, v0.Z, v1.Z, v2.Z, asciiChar);
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
                (vProj.X * 0.5f + 0.5f) * rasterizer.Width,
                (1 - (vProj.Y * 0.5f + 0.5f)) * rasterizer.Height
            );
        }

        public string GetFrame() => rasterizer.GetFrame();
    }
}
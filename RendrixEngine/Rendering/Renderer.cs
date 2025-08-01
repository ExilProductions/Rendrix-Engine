using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using RendrixEngine.Components;
using RendrixEngine.Engine;
using RendrixEngine.Mathematics;
using RendrixEngine.Models;

namespace RendrixEngine.Rendering
{
    /// <summary>
    /// High-level renderer that traverses the scene graph and delegates rasterization to a Rasterizer.
    /// </summary>
    public class Renderer
    {
        private readonly Camera camera;
        private readonly string asciiChars;
        private readonly float ambientStrength;
        private readonly Rasterizer rasterizer;

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

            // We use the Rasterizer for pixel lighting and ASCII output
            rasterizer = new Rasterizer(Width, Height, asciiChars);
        }

        public void Clear()
        {
            rasterizer.Clear();
        }

        public void Render(SceneNode rootNode)
        {
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode));

            UpdateComponents(rootNode);

            var lights = new List<Light>();
            CollectLights(rootNode, lights);

            Clear();
            RenderNode(rootNode, lights);
        }

        private void UpdateComponents(SceneNode node)
        {
            node.Components.ForEach(c => c.Update(Time.DeltaTime));
            foreach (var child in node.Children)
                UpdateComponents(child);
        }

        private void CollectLights(SceneNode node, List<Light> lights)
        {
            node.Components.ForEach(c =>
            {
                if (c is Light light)
                    lights.Add(light);
            });

            foreach (var child in node.Children)
                CollectLights(child, lights);
        }

        private void RenderNode(SceneNode node, List<Light> lights)
        {
            node.Components.ForEach(c =>
            {
                if (c is MeshRenderer meshRenderer)
                {
                    Matrix4x4 modelView = node.Transform.WorldMatrix * camera.ViewMatrix;

                    foreach (var tri in meshRenderer.Mesh.Triangles)
                    {
                        // Project vertices into camera space
                        Vector3D v0 = modelView.Transform(meshRenderer.Mesh.Vertices[tri[0]]);
                        Vector3D v1 = modelView.Transform(meshRenderer.Mesh.Vertices[tri[1]]);
                        Vector3D v2 = modelView.Transform(meshRenderer.Mesh.Vertices[tri[2]]);

                        Vector3D normalCamera = Vector3D.Cross(v1 - v0, v2 - v0).Normalized;
                        if (Vector3D.Dot(normalCamera, v0) > 0) continue;

                        // Get world-space vertices (needed for lighting)
                        Vector3D v0World = node.Transform.WorldMatrix.Transform(meshRenderer.Mesh.Vertices[tri[0]]);
                        Vector3D v1World = node.Transform.WorldMatrix.Transform(meshRenderer.Mesh.Vertices[tri[1]]);
                        Vector3D v2World = node.Transform.WorldMatrix.Transform(meshRenderer.Mesh.Vertices[tri[2]]);

                        // World-space triangle normal for flat shading
                        Vector3D normalWorld = Vector3D.Cross(v1World - v0World, v2World - v0World).Normalized;

                        // Project to 2D screen coordinates
                        Vector2D p0 = Project(v0);
                        Vector2D p1 = Project(v1);
                        Vector2D p2 = Project(v2);

                        // Forward triangle to Rasterizer (with flat normal shading)
                        rasterizer.RasterizeTriangleWithLighting(
                            p0, p1, p2,
                            v0.Z, v1.Z, v2.Z,
                            v0World, v1World, v2World,
                            normalWorld, normalWorld, normalWorld,
                            lights, ambientStrength, asciiChars);
                    }
                }
            });

            foreach (var child in node.Children)
                RenderNode(child, lights);
        }

        private Vector2D Project(Vector3D v)
        {
            Vector3D vProj = camera.ProjectionMatrix.Transform(v);
            if (vProj.Z <= 0) return new Vector2D(-1, -1);
            return new Vector2D(
                (vProj.X * 0.5f + 0.5f) * Width,
                (1 - (vProj.Y * 0.5f + 0.5f)) * Height
            );
        }

        public string GetFrame()
        {
            return rasterizer.GetFrame();
        }
    }
}

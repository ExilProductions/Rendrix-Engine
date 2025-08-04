using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using RendrixEngine.Components;
using RendrixEngine.Mathematics;
using RendrixEngine.Models;
using RendrixEngine.Systems;

namespace RendrixEngine.Rendering
{
    public class Renderer
    {
        private Camera camera;
        private readonly string asciiChars;
        private readonly float ambientStrength;
        private readonly Rasterizer rasterizer;

        public int Width { get; }
        public int Height { get; }

        public Renderer(int screenWidth, int screenHeight, Camera camera, string asciiChars, float ambientStrength, float indirectLighting = 0.2f)
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
            rasterizer = new Rasterizer(Width, Height, asciiChars, indirectLighting);
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
            node.Components.ForEach(c => c.Update());
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
                    Matrix4x4 modelMatrix = node.Transform.WorldMatrix;
                    Matrix4x4 modelViewMatrix = modelMatrix * camera.ViewMatrix;

                    Texture? texture = meshRenderer.Mesh.Texture;
                    bool hasUVs = meshRenderer.Mesh.UVs.Count > 0;

                    foreach (var tri in meshRenderer.Mesh.Triangles)
                    {
                        Vector3D v0_local = meshRenderer.Mesh.Vertices[tri[0]];
                        Vector3D v1_local = meshRenderer.Mesh.Vertices[tri[1]];
                        Vector3D v2_local = meshRenderer.Mesh.Vertices[tri[2]];

                        Vector3D v0_view = modelViewMatrix.Transform(v0_local);
                        Vector3D v1_view = modelViewMatrix.Transform(v1_local);
                        Vector3D v2_view = modelViewMatrix.Transform(v2_local);

                        Vector3D normal_view = Vector3D.Cross(v1_view - v0_view, v2_view - v0_view).Normalized;
                        if (Vector3D.Dot(normal_view, v0_view) > 0) continue;

                        Vector3D v0_world = modelMatrix.Transform(v0_local);
                        Vector3D v1_world = modelMatrix.Transform(v1_local);
                        Vector3D v2_world = modelMatrix.Transform(v2_local);

                        Vector3D n0_world = modelMatrix.TransformNormal(meshRenderer.Mesh.Normals[tri[0]]);
                        Vector3D n1_world = modelMatrix.TransformNormal(meshRenderer.Mesh.Normals[tri[1]]);
                        Vector3D n2_world = modelMatrix.TransformNormal(meshRenderer.Mesh.Normals[tri[2]]);

                        Vector2D p0 = Project(v0_view);
                        Vector2D p1 = Project(v1_view);
                        Vector2D p2 = Project(v2_view);

                        Vector2D uv0 = Vector2D.Zero, uv1 = Vector2D.Zero, uv2 = Vector2D.Zero;

                        if (texture != null && hasUVs)
                        {
                            uv0 = meshRenderer.Mesh.UVs[tri[0]];
                            uv1 = meshRenderer.Mesh.UVs[tri[1]];
                            uv2 = meshRenderer.Mesh.UVs[tri[2]];
                        }

                        rasterizer.RasterizeTriangleWithLighting(
                            p0, p1, p2,
                            v0_view.Z, v1_view.Z, v2_view.Z,
                            v0_world, v1_world, v2_world,
                            n0_world, n1_world, n2_world,
                            uv0, uv1, uv2,
                            texture,
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

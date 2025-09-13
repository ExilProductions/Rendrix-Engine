using System;
using System.Collections.Generic;
using System.Numerics;

namespace RendrixEngine
{
    public class Renderer
    {
        private readonly List<Camera> cameras;
        private readonly string asciiChars;
        private readonly float ambientStrength;
        private readonly Rasterizer rasterizer;

        public Renderer(int screenWidth, int screenHeight, string asciiChars, float ambientStrength, float indirectLighting = 0.2f)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                throw new ArgumentException("Width and height must be positive.");
            if (string.IsNullOrEmpty(asciiChars))
                throw new ArgumentException("ASCII character set cannot be empty.");
            if (ambientStrength < 0 || ambientStrength > 1)
                throw new ArgumentOutOfRangeException(nameof(ambientStrength), "Ambient strength must be between 0 and 1.");

            WindowSettings.Width = screenWidth;
            WindowSettings.Height = screenHeight;

            cameras = new List<Camera>();
            this.asciiChars = asciiChars;
            this.ambientStrength = ambientStrength;
            rasterizer = new Rasterizer(asciiChars, indirectLighting);
        }

        public void Clear()
        {
            rasterizer.Clear();
        }

        public void Render(SceneNode rootNode)
        {
            if (Camera.Main == null)
                throw new InvalidOperationException("No main camera set. Please add a Camera component and set it as Main.");
            if (rootNode == null)
                throw new ArgumentNullException(nameof(rootNode));

            Camera mainCam = Camera.Main;
            Matrix4x4 viewMatrix = mainCam.ViewMatrix;
            Matrix4x4 projectionMatrix = mainCam.ProjectionMatrix;

            UpdateComponentsIterative(rootNode);

            var lights = new List<Light>(16);
            CollectLightsIterative(rootNode, lights);

            Clear();

            var stack = new Stack<SceneNode>();
            stack.Push(rootNode);

            while (stack.Count > 0)
            {
                SceneNode node = stack.Pop();

                Matrix4x4 modelMatrix = node.Transform.WorldMatrix;
                Matrix4x4 modelViewMatrix = Matrix4x4.Multiply(modelMatrix, viewMatrix);

                var comps = node.Components;
                for (int c = 0, cn = comps.Count; c < cn; c++)
                {
                    var comp = comps[c];
                    if (comp is MeshRenderer meshRenderer)
                    {
                        var mesh = meshRenderer.Mesh;
                        if (mesh == null) continue;

                        var verts = mesh.Vertices;
                        var tris = mesh.Triangles;
                        var normals = mesh.Normals;
                        var uvs = mesh.UVs;
                        Texture? texture = mesh.Texture;
                        bool hasUVs = uvs != null && uvs.Count > 0;

                        for (int t = 0, tn = tris.Length; t < tn; t++)
                        {
                            var tri = tris[t];
                            int i0 = tri[0];
                            int i1 = tri[1];
                            int i2 = tri[2];

                            Vector3 v0_local = verts[i0];
                            Vector3 v1_local = verts[i1];
                            Vector3 v2_local = verts[i2];

                            Vector3 v0_view = modelViewMatrix.TransformPoint(v0_local);
                            Vector3 v1_view = modelViewMatrix.TransformPoint(v1_local);
                            Vector3 v2_view = modelViewMatrix.TransformPoint(v2_local);

                            Vector3 edge1 = v1_view - v0_view;
                            Vector3 edge2 = v2_view - v0_view;
                            Vector3 normal_view = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                            if (Vector3.Dot(normal_view, v0_view) > 0f) continue; // backface cull

                            if (v0_view.Z <= 0f && v1_view.Z <= 0f && v2_view.Z <= 0f) continue; // behind camera

                            Vector3 v0_world = modelMatrix.TransformPoint(v0_local);
                            Vector3 v1_world = modelMatrix.TransformPoint(v1_local);
                            Vector3 v2_world = modelMatrix.TransformPoint(v2_local);

                            Vector3 n0_world = Vector3.Normalize(modelMatrix.TransformDirection(normals[i0]));
                            Vector3 n1_world = Vector3.Normalize(modelMatrix.TransformDirection(normals[i1]));
                            Vector3 n2_world = Vector3.Normalize(modelMatrix.TransformDirection(normals[i2]));

                            Vector2 p0 = Project(v0_view, projectionMatrix);
                            Vector2 p1 = Project(v1_view, projectionMatrix);
                            Vector2 p2 = Project(v2_view, projectionMatrix);

                            bool p0Valid = p0.X >= 0f && p0.Y >= 0f;
                            bool p1Valid = p1.X >= 0f && p1.Y >= 0f;
                            bool p2Valid = p2.X >= 0f && p2.Y >= 0f;
                            if (!p0Valid && !p1Valid && !p2Valid) continue;

                            Vector2 uv0 = Vector2.Zero;
                            Vector2 uv1 = Vector2.Zero;
                            Vector2 uv2 = Vector2.Zero;
                            if (texture != null && hasUVs)
                            {
                                uv0 = uvs[i0];
                                uv1 = uvs[i1];
                                uv2 = uvs[i2];
                            }

                            rasterizer.RasterizeLit(
                                p0, p1, p2,
                                v0_view.Z, v1_view.Z, v2_view.Z,
                                v0_world, v1_world, v2_world,
                                n0_world, n1_world, n2_world,
                                uv0, uv1, uv2,
                                texture,
                                lights, ambientStrength, asciiChars);
                        }
                    }
                }

                var children = node.Children;
                for (int i = children.Count - 1; i >= 0; i--)
                    stack.Push(children[i]);
            }
        }

        private void UpdateComponentsIterative(SceneNode root)
        {
            var stack = new Stack<SceneNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var comps = node.Components;
                for (int i = 0, n = comps.Count; i < n; i++)
                {
                    if(!comps[i].Enabled)
                        continue;
                    comps[i].Update();
                }

                var children = node.Children;
                for (int i = 0; i < children.Count; i++)
                    stack.Push(children[i]);
            }
        }

        private void CollectLightsIterative(SceneNode root, List<Light> outLights)
        {
            var stack = new Stack<SceneNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                var comps = node.Components;
                for (int i = 0, n = comps.Count; i < n; i++)
                {
                    var c = comps[i];
                    if (c is Light light)
                        outLights.Add(light);
                }

                var children = node.Children;
                for (int i = 0; i < children.Count; i++)
                    stack.Push(children[i]);
            }
        }

        private Vector2 Project(Vector3 vView, Matrix4x4 projectionMatrix)
        {
            Vector3 vProj = projectionMatrix.TransformPoint(vView);
            if (vProj.Z <= 0f) return new Vector2(-1f, -1f);
            float aspectCorrection = 0.65f;
            float x = (vProj.X * 0.5f + 0.5f) * WindowSettings.Width;
            float y = (1f - (vProj.Y * 0.5f * aspectCorrection + 0.5f)) * WindowSettings.Height;
            x = ClampF(x, 0f, WindowSettings.Width);
            y = ClampF(y, 0f, WindowSettings.Height);
            return new Vector2(x, y);
        }

        private static float ClampF(float v, float a, float b)
        {
            if (v < a) return a;
            if (v > b) return b;
            return v;
        }

        public string GetFrame()
        {
            return rasterizer.GetFrame();
        }
    }
}

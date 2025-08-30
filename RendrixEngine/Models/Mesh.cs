using System.Collections.Generic;
using System;
using System.Linq;

namespace RendrixEngine
{
    public class Mesh
    {
        public List<Vector3D> Vertices { get; }
        public List<Vector3D> Normals { get; }
        public List<Vector2D> UVs { get; }
        public Texture? Texture { get; set; }
        public int[][] Triangles { get; }

        public Mesh()
        {
            Vertices = new List<Vector3D>();
            Normals = new List<Vector3D>();
            UVs = new List<Vector2D>();
            Triangles = Array.Empty<int[]>();
        }

        public Mesh(List<Vector3D> vertices, List<Vector3D> normals, List<Vector2D> uvs, int[][] triangles, Texture? texture = null)
        {
            if (vertices == null || vertices.Count == 0)
                throw new ArgumentException("Vertices array cannot be null or empty.", nameof(vertices));
            if (normals == null || normals.Count != vertices.Count)
                throw new ArgumentException("Normals must match vertex count.", nameof(normals));
            if (uvs == null || uvs.Count != vertices.Count)
                throw new ArgumentException("UVs must match vertex count.", nameof(uvs));
            if (triangles == null || triangles.Length == 0)
                throw new ArgumentException("Triangles array cannot be null or empty.", nameof(triangles));

            ValidateTriangles(vertices.Count, triangles);

            Vertices = vertices;
            Normals = normals;
            UVs = uvs;
            Triangles = triangles;
            Texture = texture;
        }

        private static void ValidateTriangles(int vertexCount, int[][] triangles)
        {
            var triangleSet = new HashSet<(int, int, int)>();
            foreach (var tri in triangles)
            {
                if (tri == null || tri.Length != 3)
                    throw new ArgumentException("Each triangle must contain exactly 3 indices.");

                foreach (var idx in tri)
                {
                    if (idx < 0 || idx >= vertexCount)
                        throw new ArgumentOutOfRangeException(nameof(triangles), $"Index {idx} out of bounds.");
                }

                var sorted = tri.OrderBy(i => i).ToArray();
                var key = (sorted[0], sorted[1], sorted[2]);
                if (!triangleSet.Add(key))
                    throw new ArgumentException("Duplicate triangle detected.");
            }
        }

        public static Mesh CreateCube(float size)
        {
            float s = size / 2.0f;

            var vertices = new List<Vector3D>
            {
                // Front face
                new Vector3D(-s, -s, -s), new Vector3D(s, -s, -s), new Vector3D(s, s, -s), new Vector3D(-s, s, -s),
                // Back face
                new Vector3D(s, -s, s), new Vector3D(-s, -s, s), new Vector3D(-s, s, s), new Vector3D(s, s, s),
                // Top face
                new Vector3D(-s, s, -s), new Vector3D(s, s, -s), new Vector3D(s, s, s), new Vector3D(-s, s, s),
                // Bottom face
                new Vector3D(-s, -s, s), new Vector3D(s, -s, s), new Vector3D(s, -s, -s), new Vector3D(-s, -s, -s),
                // Right face
                new Vector3D(s, -s, -s), new Vector3D(s, -s, s), new Vector3D(s, s, s), new Vector3D(s, s, -s),
                // Left face
                new Vector3D(-s, -s, s), new Vector3D(-s, -s, -s), new Vector3D(-s, s, -s), new Vector3D(-s, s, s)
            };

            var normals = new List<Vector3D>
            {
                Vector3D.Back, Vector3D.Back, Vector3D.Back, Vector3D.Back,
                Vector3D.Forward, Vector3D.Forward, Vector3D.Forward, Vector3D.Forward,
                Vector3D.Up, Vector3D.Up, Vector3D.Up, Vector3D.Up,
                Vector3D.Down, Vector3D.Down, Vector3D.Down, Vector3D.Down,
                Vector3D.Right, Vector3D.Right, Vector3D.Right, Vector3D.Right,
                Vector3D.Left, Vector3D.Left, Vector3D.Left, Vector3D.Left
            };

            var uvs = new List<Vector2D>();
            for (int i = 0; i < 6; i++)
            {
                uvs.Add(new Vector2D(0, 1));
                uvs.Add(new Vector2D(1, 1));
                uvs.Add(new Vector2D(1, 0));
                uvs.Add(new Vector2D(0, 0));
            }

            var triangles = new int[][]
            {
                new[] { 0, 1, 2 }, new[] { 0, 2, 3 },     // Front
                new[] { 4, 5, 6 }, new[] { 4, 6, 7 },     // Back
                new[] { 8, 9,10 }, new[] { 8,10,11 },     // Top
                new[] {12,13,14 }, new[] {12,14,15 },     // Bottom
                new[] {16,17,18 }, new[] {16,18,19 },     // Right
                new[] {20,21,22 }, new[] {20,22,23 }      // Left
            };

            return new Mesh(vertices, normals, uvs, triangles);
        }

        public static Mesh CreateSphere(float radius, int latitudeBands = 20, int longitudeBands = 20)
        {
            if (latitudeBands < 2)
                throw new ArgumentOutOfRangeException(nameof(latitudeBands), "Latitude bands must be >= 2.");
            if (longitudeBands < 3)
                throw new ArgumentOutOfRangeException(nameof(longitudeBands), "Longitude bands must be >= 3.");

            var vertices = new List<Vector3D>();
            var normals = new List<Vector3D>();
            var uvs = new List<Vector2D>();
            var triangles = new List<int[]>();

            for (int lat = 0; lat <= latitudeBands; lat++)
            {
                double theta = lat * Math.PI / latitudeBands;
                double sinTheta = Math.Sin(theta);
                double cosTheta = Math.Cos(theta);

                for (int lon = 0; lon <= longitudeBands; lon++)
                {
                    double phi = lon * 2 * Math.PI / longitudeBands;
                    double sinPhi = Math.Sin(phi);
                    double cosPhi = Math.Cos(phi);

                    float x = (float)(cosPhi * sinTheta);
                    float y = (float)(cosTheta);
                    float z = (float)(sinPhi * sinTheta);

                    var pos = new Vector3D(x * radius, y * radius, z * radius);
                    vertices.Add(pos);
                    normals.Add(new Vector3D(x, y, z).Normalized);
                    uvs.Add(new Vector2D((float)lon / longitudeBands, 1.0f - (float)lat / latitudeBands));
                }
            }

            int stride = longitudeBands + 1;

            for (int lat = 0; lat < latitudeBands; lat++)
            {
                for (int lon = 0; lon < longitudeBands; lon++)
                {
                    int i0 = lat * stride + lon;
                    int i1 = i0 + stride;
                    int i2 = i0 + 1;
                    int i3 = i1 + 1;

                    triangles.Add(new[] { i0, i1, i2 });
                    triangles.Add(new[] { i2, i1, i3 });
                }
            }

            return new Mesh(vertices, normals, uvs, triangles.ToArray());
        }
    }
}

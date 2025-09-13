using System.Numerics;
namespace RendrixEngine
{
    public class Mesh
    {
        public List<Vector3> Vertices { get; }
        public List<Vector3> Normals { get; }
        public List<Vector2> UVs { get; }
        public Texture? Texture { get; set; }
        public int[][] Triangles { get; }

        public Mesh()
        {
            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector2>();
            Triangles = Array.Empty<int[]>();
        }

        public Mesh(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, int[][] triangles, Texture? texture = null)
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

            var vertices = new List<Vector3>
            {
                // Front face
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
                // Back face
                new Vector3(s, -s, s), new Vector3(-s, -s, s), new Vector3(-s, s, s), new Vector3(s, s, s),
                // Top face
                new Vector3(-s, s, -s), new Vector3(s, s, -s), new Vector3(s, s, s), new Vector3(-s, s, s),
                // Bottom face
                new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, -s, -s), new Vector3(-s, -s, -s),
                // Right face
                new Vector3(s, -s, -s), new Vector3(s, -s, s), new Vector3(s, s, s), new Vector3(s, s, -s),
                // Left face
                new Vector3(-s, -s, s), new Vector3(-s, -s, -s), new Vector3(-s, s, -s), new Vector3(-s, s, s)
            };

            var normals = new List<Vector3>
            {
                Vector3.UnitZ * -1, Vector3.UnitZ * -1, Vector3.UnitZ * -1, Vector3.UnitZ * -1, // Back
                Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ,                   // Forward
                Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY,                   // Up
                Vector3.UnitY * -1, Vector3.UnitY * -1, Vector3.UnitY * -1, Vector3.UnitY * -1, // Down
                Vector3.UnitX, Vector3.UnitX, Vector3.UnitX, Vector3.UnitX,                   // Right
                Vector3.UnitX * -1, Vector3.UnitX * -1, Vector3.UnitX * -1, Vector3.UnitX * -1  // Left
            };

            var uvs = new List<Vector2>();
            for (int i = 0; i < 6; i++)
            {
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
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

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
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

                    var pos = new Vector3(x * radius, y * radius, z * radius);
                    vertices.Add(pos);

                    normals.Add(Vector3.Normalize(new Vector3(x, y, z)));
                    uvs.Add(new Vector2((float)lon / longitudeBands, 1.0f - (float)lat / latitudeBands));
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

using RendrixEngine.Mathematics;

namespace RendrixEngine.Models
{
    public class Mesh
    {
        public string Name { get; set; } = string.Empty;
        public List<Vector3D> Vertices { get; }
        public List<Vector3D> Normals { get; }
        public List<int> Indices { get; } = new List<int>();
        public int[][] Triangles { get; }

        public Mesh() { }

        public Mesh(List<Vector3D> vertices, List<Vector3D> normals, int[][] triangles)
        {
            if (vertices == null || vertices.Count == 0)
                throw new ArgumentException("Vertices array cannot be null or empty.", nameof(vertices));
            if (normals == null || normals.Count == 0 || normals.Count != vertices.Count)
                throw new ArgumentException("Normals array cannot be null or empty and must match vertex count.", nameof(normals));
            if (triangles == null || triangles.Length == 0)
                throw new ArgumentException("Triangles array cannot be null or empty.", nameof(triangles));

            var triangleSet = new HashSet<(int, int, int)>();
            foreach (var tri in triangles)
            {
                if (tri == null || tri.Length != 3)
                    throw new ArgumentException("Each triangle must have exactly 3 indices.", nameof(triangles));
                foreach (var index in tri)
                {
                    if (index < 0 || index >= vertices.Count)
                        throw new ArgumentOutOfRangeException(nameof(triangles), "Triangle index out of bounds.");
                }
                var sortedTri = tri.OrderBy(i => i).ToArray();
                var triTuple = (sortedTri[0], sortedTri[1], sortedTri[2]);
                if (!triangleSet.Add(triTuple))
                {
                    throw new ArgumentException("Duplicate triangle detected.", nameof(triangles));
                }
            }

            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
        }

        public static Mesh CreateCube(float size)
        {
            float s = size / 2.0f;

            Vector3D[] vertices = new Vector3D[]
            {
                new Vector3D(-s, -s, -s), // 0
                new Vector3D(s, -s, -s),  // 1
                new Vector3D(s, s, -s),   // 2
                new Vector3D(-s, s, -s),  // 3
                new Vector3D(-s, -s, s),  // 4
                new Vector3D(s, -s, s),   // 5
                new Vector3D(s, s, s),    // 6
                new Vector3D(-s, s, s)    // 7
            };

            Vector3D[] normals = new Vector3D[]
            {
                new Vector3D(-1, -1, -1).Normalized, // 0
                new Vector3D( 1, -1, -1).Normalized, // 1
                new Vector3D( 1,  1, -1).Normalized, // 2
                new Vector3D(-1,  1, -1).Normalized, // 3
                new Vector3D(-1, -1,  1).Normalized, // 4
                new Vector3D( 1, -1,  1).Normalized, // 5
                new Vector3D( 1,  1,  1).Normalized, // 6
                new Vector3D(-1,  1,  1).Normalized  // 7
            };


            int[][] triangles = new int[][]
            {
                //ff
                new int[] { 0, 1, 2 }, new int[] { 0, 2, 3 },
                //bf
                new int[] { 4, 6, 5 }, new int[] { 4, 7, 6 },
                //lf
                new int[] { 0, 3, 7 }, new int[] { 0, 7, 4 },
                //rf
                new int[] { 1, 5, 6 }, new int[] { 1, 6, 2 },
                //tf
                new int[] { 3, 2, 6 }, new int[] { 3, 6, 7 },
                //btmf
                new int[] { 0, 4, 5 }, new int[] { 0, 5, 1 }
            };

            return new Mesh(vertices.ToList(), normals.ToList(), triangles);
        }

        public static Mesh CreateSphere(float radius, int latitudeBands = 20, int longitudeBands = 20)
        {
            var vertices = new List<Vector3D>();
            var normals = new List<Vector3D>();
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

                    float x = (float)(radius * cosPhi * sinTheta);
                    float y = (float)(radius * cosTheta);
                    float z = (float)(radius * sinPhi * sinTheta);


                    Vector3D vertex = new Vector3D(x, y, z);
                    vertices.Add(vertex);
                    normals.Add(vertex.Normalized);
                }
            }

            for (int lat = 0; lat < latitudeBands; lat++)
            {
                for (int lon = 0; lon < longitudeBands; lon++)
                {
                    int first = (lat * (longitudeBands + 1)) + lon;
                    int second = first + longitudeBands + 1;

                    triangles.Add(new int[] { first, second, first + 1 });
                    triangles.Add(new int[] { first + 1, second, second + 1 });
                }
            }

            return new Mesh(vertices, normals, triangles.ToArray());
        }
    }
}
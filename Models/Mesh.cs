// Mesh.cs
using System;
using System.Collections.Generic;
using System.Linq;
using RendrixEngine.Mathematics;

namespace RendrixEngine.Models
{
    /// <summary>
    /// Represents a 3D mesh with vertices, triangle indices, and now, vertex normals.
    /// </summary>
    public class Mesh
    {
        public Vector3D[] Vertices { get; }
        public Vector3D[] Normals { get; } // Array to store vertex normals
        public int[][] Triangles { get; }

        public Mesh(Vector3D[] vertices, Vector3D[] normals, int[][] triangles) // Modified constructor
        {
            if (vertices == null || vertices.Length == 0)
                throw new ArgumentException("Vertices array cannot be null or empty.", nameof(vertices));
            if (normals == null || normals.Length == 0 || normals.Length != vertices.Length) // Validate normals
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
                    if (index < 0 || index >= vertices.Length)
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
            Normals = normals; // Assign normals
            Triangles = triangles;
        }

        /// <summary>
        /// Creates a simple cube mesh with calculated vertex normals.
        /// </summary>
        /// <param name="size">The size of the cube.</param>
        public static Mesh CreateCube(float size)
        {
            float s = size / 2.0f;

            // Vertices
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
                // Front face
                new int[] { 0, 1, 2 }, new int[] { 0, 2, 3 },
                // Back face
                new int[] { 4, 6, 5 }, new int[] { 4, 7, 6 },
                // Left face
                new int[] { 0, 3, 7 }, new int[] { 0, 7, 4 },
                // Right face
                new int[] { 1, 5, 6 }, new int[] { 1, 6, 2 },
                // Top face
                new int[] { 3, 2, 6 }, new int[] { 3, 6, 7 },
                // Bottom face
                new int[] { 0, 4, 5 }, new int[] { 0, 5, 1 }
            };

            return new Mesh(vertices, normals, triangles); // Pass normals to the constructor
        }

        /// <summary>
        /// Creates a sphere mesh with calculated vertex positions and normals.
        /// </summary>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="latitudeBands">Number of divisions along the latitude.</param>
        /// <param name="longitudeBands">Number of divisions along the longitude.</param>
        /// <returns>A new Sphere Mesh.</returns>
        public static Mesh CreateSphere(float radius, int latitudeBands = 20, int longitudeBands = 20)
        {
            var vertices = new List<Vector3D>();
            var normals = new List<Vector3D>(); //Store normals for sphere
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
                    normals.Add(vertex.Normalized); // For a sphere the normalized vertex position is its normal
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

            return new Mesh(vertices.ToArray(), normals.ToArray(), triangles.ToArray()); // Pass normals
        }
    }
}
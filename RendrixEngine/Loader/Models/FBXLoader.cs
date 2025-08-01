using RendrixEngine.Mathematics;
using RendrixEngine.Models;
using System.IO.Compression;
using System.Text;

namespace RendrixEngine.Loader.Models
{
    public class FBXLoader
    {
        private const string ExpectedHeader = "Kaydara FBX Binary  \0";
        private const int HeaderLength = 21;
        private const int UnknownHeaderBytes = 2;
        private const int NullRecordLength = 13;
        private const int ZLibEncoding = 1;
        private const int ZLibHeaderOffset = 2;
        private const int VertexComponentCount = 3;

        public static List<Mesh> Load(byte[] fbxData)
        {
            using (var stream = new MemoryStream(fbxData))
            using (var reader = new BinaryReader(stream))
            {
                string headerString = Encoding.ASCII.GetString(reader.ReadBytes(HeaderLength));
                if (headerString != ExpectedHeader)
                {
                    throw new Exception("Not a valid binary FBX file. Header mismatch.");
                }

                reader.ReadBytes(UnknownHeaderBytes);

                uint version = reader.ReadUInt32();

                var meshes = new List<Mesh>();

                while (stream.Position < stream.Length)
                {
                    var node = ReadNode(reader);
                    if (node == null) break;

                    if (node.Name == "Objects")
                    {
                        ParseObjects(node, meshes);
                    }
                }

                return meshes;
            }
        }

        private static FBXNode ReadNode(BinaryReader reader)
        {
            long startPosition = reader.BaseStream.Position;

            uint endOffset = reader.ReadUInt32();
            if (endOffset == 0) return null;

            uint numProperties = reader.ReadUInt32();
            uint propertyListLen = reader.ReadUInt32();
            byte nameLen = reader.ReadByte();
            string name = Encoding.ASCII.GetString(reader.ReadBytes(nameLen));

            var node = new FBXNode { Name = name };

            for (int i = 0; i < numProperties; i++)
            {
                node.Properties.Add(ReadProperty(reader));
            }

            if (reader.BaseStream.Position < endOffset)
            {
                while (reader.BaseStream.Position < endOffset - NullRecordLength)
                {
                    var childNode = ReadNode(reader);
                    if (childNode != null)
                    {
                        node.Children.Add(childNode);
                    }
                }
                reader.ReadBytes(NullRecordLength);
            }

            return node;
        }

        private static T[] ReadArray<T>(BinaryReader reader, int elementSize) where T : struct
        {
            uint arrayLength = reader.ReadUInt32();
            uint encoding = reader.ReadUInt32();
            uint compressedLength = reader.ReadUInt32();

            byte[] data = reader.ReadBytes((int)compressedLength);

            if (encoding == ZLibEncoding)
            {
                using (var memoryStream = new MemoryStream(data, ZLibHeaderOffset, data.Length - ZLibHeaderOffset))
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                {
                    var decompressed = new MemoryStream();
                    deflateStream.CopyTo(decompressed);
                    data = decompressed.ToArray();
                }
            }

            T[] result = new T[arrayLength];
            Buffer.BlockCopy(data, 0, result, 0, (int)arrayLength * elementSize);
            return result;
        }

        private static object ReadProperty(BinaryReader reader)
        {
            char type = (char)reader.ReadByte();
            switch (type)
            {
                case 'S':
                    int len = reader.ReadInt32();
                    return Encoding.ASCII.GetString(reader.ReadBytes(len));
                case 'I': return reader.ReadInt32();
                case 'L': return reader.ReadInt64();
                case 'D': return reader.ReadDouble();
                case 'F': return reader.ReadSingle();
                case 'C': return reader.ReadBoolean();
                case 'Y': return reader.ReadInt16();
                case 'd': return ReadArray<double>(reader, sizeof(double));
                case 'i': return ReadArray<int>(reader, sizeof(int));
                case 'l': return ReadArray<long>(reader, sizeof(long));
                case 'f': return ReadArray<float>(reader, sizeof(float));
                case 'b': return ReadArray<bool>(reader, sizeof(bool));
                default:
                    throw new Exception($"Unsupported FBX property type: {type}");
            }
        }

        private static void ParseObjects(FBXNode objectsNode, List<Mesh> meshes)
        {
            foreach (var childNode in objectsNode.Children)
            {
                if (childNode.Name == "Geometry")
                {
                    var mesh = new Mesh();
                    mesh.Name = ((string)childNode.Properties[1]).Split('\0')[0];

                    foreach (var geoChild in childNode.Children)
                    {
                        if (geoChild.Name == "Vertices" && geoChild.Properties[0] is double[] vertices)
                        {
                            for (int i = 0; i < vertices.Length; i += VertexComponentCount)
                            {
                                mesh.Vertices.Add(new Vector3D((float)vertices[i], (float)vertices[i + 1], (float)vertices[i + 2]));
                            }
                        }
                        else if (geoChild.Name == "PolygonVertexIndex" && geoChild.Properties[0] is int[] indices)
                        {
                            foreach (var index in indices)
                            {
                                mesh.Indices.Add(index < 0 ? -index - 1 : index);
                            }
                        }
                    }
                    meshes.Add(mesh);
                }
            }
        }
    }
}

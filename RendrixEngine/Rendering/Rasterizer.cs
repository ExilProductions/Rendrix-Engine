using System.Numerics;

namespace RendrixEngine
{

    internal class Rasterizer
    {
        private readonly char[] screenBuffer;
        private readonly float[] zBuffer;
        private readonly char[] asciiCharsArr;
        private readonly int width;
        private readonly int height;
        private readonly int rowStride;
        private const float FalloffConstant = 0.1f;
        private float previousAverageBrightness = 0f;
        private readonly float indirectFactor;
        private readonly int asciiMaxIndex;


        public Rasterizer(string asciiChars, float indirectFactor = 0.2f)
        {
            if (WindowSettings.Width <= 0 || WindowSettings.Height <= 0)
                throw new ArgumentException("Width and height must be positive.");
            if (string.IsNullOrEmpty(asciiChars))
                throw new ArgumentException("ASCII character set cannot be empty.");

            this.width = WindowSettings.Width;
            this.height = WindowSettings.Height;
            this.rowStride = this.width + 1;
            this.screenBuffer = new char[this.rowStride * this.height];
            this.zBuffer = new float[this.width * this.height];
            this.asciiCharsArr = asciiChars.ToCharArray();
            this.asciiMaxIndex = Math.Max(1, asciiCharsArr.Length - 1);
            this.indirectFactor = indirectFactor;

            Clear();
        }


        public void Clear()
        {
            Array.Fill(zBuffer, float.MaxValue);
            for (int r = 0; r < height; r++)
            {
                int baseIdx = r * rowStride;

                for (int c = 0; c < width; c++)
                    screenBuffer[baseIdx + c] = ' ';

                screenBuffer[baseIdx + width] = '\n';
            }
        }


        private int ZIndex(int x, int y) => y * width + x;
        private int ScreenIndex(int x, int y) => y * rowStride + x;


        public void RasterizeLit(
            Vector2 p0, Vector2 p1, Vector2 p2,
            float z0, float z1, float z2,
            Vector3 worldPos0, Vector3 worldPos1, Vector3 worldPos2,
            Vector3 normal0, Vector3 normal1, Vector3 normal2,
            Vector2 uv0, Vector2 uv1, Vector2 uv2,
            Texture? texture,
            List<Light> lights, float ambientStrength, string asciiChars)
        {

            float minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
            float maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
            float minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
            float maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));

            int startX = Math.Max(0, (int)Math.Floor(minX));
            int endX = Math.Min(width - 1, (int)Math.Ceiling(maxX));
            int startY = Math.Max(0, (int)Math.Floor(minY));
            int endY = Math.Min(height - 1, (int)Math.Ceiling(maxY));


            float area = (p1.Y - p2.Y) * (p0.X - p2.X) + (p2.X - p1.X) * (p0.Y - p2.Y);
            if (Math.Abs(area) < 1e-6f) return;
            float invArea = 1.0f / area;


            float A0 = (p1.Y - p2.Y);
            float B0 = (p2.X - p1.X);
            float C0 = p1.X * p2.Y - p2.X * p1.Y;

            float A1 = (p2.Y - p0.Y);
            float B1 = (p0.X - p2.X);
            float C1 = p2.X * p0.Y - p0.X * p2.Y;

            float A2 = (p0.Y - p1.Y);
            float B2 = (p1.X - p0.X);
            float C2 = p0.X * p1.Y - p1.X * p0.Y;

            bool hasTexture = texture != null;
            int texW = hasTexture ? texture!.Width : 0;
            int texH = hasTexture ? texture!.Height : 0;


            int localWidth = width;
            int localRowStride = rowStride;
            char[] localScreen = screenBuffer;
            float[] localZ = zBuffer;
            char[] localAscii = asciiCharsArr;
            int localAsciiMax = asciiMaxIndex;
            float prevAvg = previousAverageBrightness;
            float localIndirect = indirectFactor;


            for (int y = startY; y <= endY; y++)
            {

                float py = y + 0.5f;
                float ex0 = A0 * (startX + 0.5f) + B0 * py + C0;
                float ex1 = A1 * (startX + 0.5f) + B1 * py + C1;
                float ex2 = A2 * (startX + 0.5f) + B2 * py + C2;

                for (int x = startX; x <= endX; x++)
                {

                    bool inside = (ex0 * area >= 0f) && (ex1 * area >= 0f) && (ex2 * area >= 0f);
                    if (inside)
                    {
                        int zi = y * localWidth + x;

                        float w0 = ex0 * invArea;
                        float w1 = ex1 * invArea;
                        float w2 = ex2 * invArea;


                        float z = w0 * z0 + w1 * z1 + w2 * z2;
                        if (z < localZ[zi])
                        {
                            localZ[zi] = z;


                            Vector3 worldPos = worldPos0 * w0 + worldPos1 * w1 + worldPos2 * w2;
                            Vector3 normal = Vector3.Normalize((normal0 * w0 + normal1 * w1 + normal2 * w2));


                            float lightingBrightness = CalculateLighting(worldPos, normal, lights, ambientStrength, prevAvg, localIndirect);

                            float finalBrightness;
                            if (hasTexture)
                            {
                                Vector2 uv = uv0 * w0 + uv1 * w1 + uv2 * w2;
                                float u_clamped = ClampF(uv.X, 0f, 1f);
                                float v_clamped = ClampF(uv.Y, 0f, 1f);
                                int tx = (int)(u_clamped * (texW - 1));
                                int ty = (int)(v_clamped * (texH - 1));
                                var texColor = texture!.GetPixel(tx, ty);
                                float texBrightness = (texColor.R / 255f * 0.299f) + (texColor.G / 255f * 0.587f) + (texColor.B / 255f * 0.114f);
                                finalBrightness = texBrightness * lightingBrightness;
                            }
                            else
                            {
                                finalBrightness = lightingBrightness;
                            }

                            finalBrightness = ClampF(finalBrightness, 0f, 1f);
                            int asciiIdx = (int)(finalBrightness * localAsciiMax);
                            localScreen[y * localRowStride + x] = localAscii[asciiIdx];
                        }
                    }


                    ex0 += A0;
                    ex1 += A1;
                    ex2 += A2;
                }
            }
        }


        private float CalculateLighting(Vector3 worldPos, Vector3 normal, List<Light> lights, float ambientStrength, float previousAvg, float indirectFactorLocal)
        {
            float brightness = ambientStrength;

            for (int i = 0; i < lights.Count; i++)
            {
                var light = lights[i];
                float diffuse = 0f;
                if (light.Type == LightType.Directional)
                {
                    diffuse = Math.Max(0f, Vector3.Dot(normal, light.Direction)) * light.Intensity;
                }
                else
                {
                    Vector3 lightDir = light.Transform.Position - worldPos;
                    float distance = lightDir.Length();
                    if (distance <= light.Range)
                    {
                        lightDir = Vector3.Normalize(lightDir);
                        float falloff = light.Intensity / (distance * distance + FalloffConstant);
                        diffuse = Math.Max(0f, Vector3.Dot(normal, lightDir)) * falloff;
                    }
                }

                brightness += diffuse;
            }

            float indirectLighting = indirectFactorLocal * previousAvg;
            brightness += indirectLighting;

            return brightness;
        }


        public string GetFrame()
        {
            float totalBrightness = 0f;
            int pixelCount = width * height;
            int asciiLen = asciiCharsArr.Length;

            for (int y = 0; y < height; y++)
            {
                int rowBase = y * rowStride;
                for (int x = 0; x < width; x++)
                {
                    char c = screenBuffer[rowBase + x];
                    if (c != ' ')
                    {

                        for (int k = 0; k < asciiLen; k++)
                        {
                            if (asciiCharsArr[k] == c)
                            {
                                totalBrightness += (float)k / (asciiLen - 1);
                                break;
                            }
                        }
                    }
                }
            }

            previousAverageBrightness = totalBrightness / pixelCount;
            return new string(screenBuffer);
        }


        private static float ClampF(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
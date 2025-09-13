using System;
using System.Numerics;

namespace RendrixEngine
{
    public static class Matrix4x4Extension
    {
        public static Vector3 TransformPoint(this Matrix4x4 matrix, Vector3 v)
        {
            Vector4 result = Vector4.Transform(new Vector4(v, 1), matrix);
            if (Math.Abs(result.W) < 1e-6f)
                throw new InvalidOperationException("Invalid transformation: w-component is zero.");
            return new Vector3(result.X / result.W, result.Y / result.W, result.Z / result.W);
        }

        public static Vector3 TransformDirection(this Matrix4x4 matrix, Vector3 n)
        {
            Vector4 result = Vector4.Transform(new Vector4(n, 0f), matrix);
            return new Vector3(result.X, result.Y, result.Z);
        }

        public static Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(eye - target);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
            Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

            return new Matrix4x4(
                xAxis.X, yAxis.X, -zAxis.X, 0,
                xAxis.Y, yAxis.Y, -zAxis.Y, 0,
                xAxis.Z, yAxis.Z, -zAxis.Z, 0,
                -Vector3.Dot(xAxis, eye), -Vector3.Dot(yAxis, eye), Vector3.Dot(zAxis, eye), 1
            );
        }

        public static Matrix4x4 CreatePerspective(float fov, float aspect, float near, float far)
        {
            if (fov <= 0 || fov >= Math.PI)
                throw new ArgumentOutOfRangeException(nameof(fov), "Field of view must be between 0 and 180 degrees.");
            if (aspect <= 0)
                throw new ArgumentOutOfRangeException(nameof(aspect), "Aspect ratio must be positive.");
            if (near <= 0 || far <= near)
                throw new ArgumentOutOfRangeException("Invalid near/far plane values.");

            float f = 1.0f / (float)Math.Tan(fov / 2);
            return new Matrix4x4(
                f / aspect, 0, 0, 0,
                0, f, 0, 0,
                0, 0, (near + far) / (near - far), 2 * near * far / (near - far),
                0, 0, -1, 0
            );
        }

        public static Matrix4x4 CreateRotationY(float angle)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            return new Matrix4x4(
                cos, 0, sin, 0,
                0, 1, 0, 0,
                -sin, 0, cos, 0,
                0, 0, 0, 1
            );
        }
    }
}

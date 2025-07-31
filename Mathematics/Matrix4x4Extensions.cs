using System;
using System.Numerics;

namespace RendrixEngine.Mathematics
{
    /// <summary>
    /// Extensions for Matrix4x4 to support transformation operations.
    /// </summary>
    public static class Matrix4x4Extensions
    {
        public static Vector3D Transform(this Matrix4x4 matrix, Vector3D v)
        {
            Vector4 result = Vector4.Transform(new Vector4(v.ToVector3(), 1), matrix);
            if (Math.Abs(result.W) < 1e-6f)
                throw new InvalidOperationException("Invalid transformation: w-component is zero.");
            return new Vector3D(result.X / result.W, result.Y / result.W, result.Z / result.W);
        }

        public static Matrix4x4 CreateLookAt(Vector3D eye, Vector3D target, Vector3D up)
        {
            Vector3D zAxis = (eye - target).Normalized;
            Vector3D xAxis = Vector3D.Cross(up, zAxis).Normalized;
            Vector3D yAxis = Vector3D.Cross(zAxis, xAxis);

            return new Matrix4x4(
                xAxis.X, yAxis.X, -zAxis.X, 0,
                xAxis.Y, yAxis.Y, -zAxis.Y, 0,
                xAxis.Z, yAxis.Z, -zAxis.Z, 0,
                -Vector3D.Dot(xAxis, eye), -Vector3D.Dot(yAxis, eye), Vector3D.Dot(zAxis, eye), 1
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
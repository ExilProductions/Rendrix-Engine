﻿using System;
using System.Numerics;

namespace RendrixEngine
{
    public class Camera
    {
        public Vector3D Position { get; set; }
        public Vector3D Target { get; set; }
        public Vector3D Up { get; set; }
        public float Fov { get; }
        public float AspectRatio { get; }
        public float NearPlane { get; }
        public float FarPlane { get; }
        public Matrix4x4 ViewMatrix { get; }
        public Matrix4x4 ProjectionMatrix { get; }

        public static Camera main;

        public Camera(Vector3D position, Vector3D target, Vector3D up, float fov, float aspectRatio, float nearPlane, float farPlane)
        {
            if ((target - position).Normalized == up.Normalized)
                throw new ArgumentException("Target cannot be aligned with up vector.");

            Position = position;
            Target = target;
            Up = up;
            Fov = fov;
            AspectRatio = aspectRatio;
            NearPlane = nearPlane;
            FarPlane = farPlane;
            ViewMatrix = Matrix4x4Extension.CreateLookAt(position, target, up);
            ProjectionMatrix = Matrix4x4Extension.CreatePerspective(fov, aspectRatio, nearPlane, farPlane);
        }
    }
}
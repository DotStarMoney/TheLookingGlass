using Microsoft.Xna.Framework;

namespace TheLookingGlass.Math
{
    public class Transform
    {
        public const float DEFAULT_FAR_PLANE_DISTANCE = 100.0f;

        public Matrix TransformMatrix
        {
            get => transformMatrix;
            private set => transformMatrix = value;
        }

        private Matrix transformMatrix;

        public static Transform Rotate(float yaw, float pitch, float roll)
        {
            return new Transform(Matrix.CreateFromYawPitchRoll(yaw, pitch, roll));
        }

        public static Transform Rotate(in Quaternion quanternion)
        {
            return new Transform(Matrix.CreateFromQuaternion(quanternion));
        }

        public static Transform Rotate(in Vector3 facing, in Vector3 up)
        {
            return new Transform(Matrix.CreateLookAt(Vector3.Zero, facing, up));
        }

        public static Transform Scale(float xScale, float yScale, float zScale)
        {
            return new Transform(Matrix.CreateScale(xScale, yScale, zScale));
        }

        public static Transform Translate(float xDelta, float yDelta, float zDelta)
        {
            return new Transform(Matrix.CreateTranslation(xDelta, yDelta, zDelta));
        }

        public static Transform Perspective(
            float width, float height, float nearPlane, float farPlane = DEFAULT_FAR_PLANE_DISTANCE)
        {
            return new Transform(Matrix.CreatePerspective(width, height, nearPlane, farPlane));
        }

        public static Transform Orthographic(
            float width, float height, float nearPlane, float farPlane = DEFAULT_FAR_PLANE_DISTANCE)
        {
            return new Transform(Matrix.CreateOrthographic(width, height, nearPlane, farPlane));
        }

        public static Transform Identity()
        {
            return new Transform(Matrix.Identity);
        }

        private Transform(in Matrix matrix) => this.TransformMatrix = matrix;

        public Transform ThenRotate(float yaw, float pitch, float roll)
        {
            transformMatrix *= Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
            return this;
        }

        public Transform ThenRotate(in Quaternion quanternion)
        {
            transformMatrix *= Matrix.CreateFromQuaternion(quanternion);
            return this;
        }

        public Transform ThenRotate(in Vector3 facing, in Vector3 up)
        {
            transformMatrix *= Matrix.CreateLookAt(Vector3.Zero, facing, up);
            return this;
        }

        public Transform ThenScale(float xScale, float yScale, float zScale)
        {
            transformMatrix *= Matrix.CreateScale(xScale, yScale, zScale);
            return this;
        }

        public Transform ThenTranslate(float xDelta, float yDelta, float zDelta)
        {
            transformMatrix *= Matrix.CreateTranslation(xDelta, yDelta, zDelta);
            return this;
        }

        public Transform ThenPerspective(
            float width, float height, float nearPlane, float farPlane = DEFAULT_FAR_PLANE_DISTANCE)
        {
            transformMatrix *= Matrix.CreatePerspective(width, height, nearPlane, farPlane);
            return this;
        }

        public Transform ThenOrthographic(
            float width, float height, float nearPlane, float farPlane = DEFAULT_FAR_PLANE_DISTANCE)
        {
            transformMatrix *= Matrix.CreateOrthographic(width, height, nearPlane, farPlane);
            return this;
        }
    }
}

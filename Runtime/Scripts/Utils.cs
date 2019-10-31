using System;
using UnityEngine;

namespace VLB
{
    public static class Utils
    {
        public static T NewWithComponent<T>(string name) where T : Component
        {
            return (new GameObject(name, typeof(T))).GetComponent<T>();
        }

        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            var component = self.GetComponent<T>();
            if (component == null)
                component = self.AddComponent<T>();
            return component;
        }

        public static T GetOrAddComponent<T>(this MonoBehaviour self) where T : Component
        {
            return self.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// true if the bit field or bit fields that are set in flags are also set in the current instance; otherwise, false.
        /// </summary>
        public static bool HasFlag(this Enum mask, Enum flags) // Same behavior than Enum.HasFlag is .NET 4
        {
#if DEBUG
            if (mask.GetType() != flags.GetType())
                throw new System.ArgumentException(string.Format("The argument type, '{0}', is not the same as the enum type '{1}'.", flags.GetType(), mask.GetType()));
#endif
            return ((int)(IConvertible)mask & (int)(IConvertible)flags) == (int)(IConvertible)flags;
        }

        public static Vector2 xy(this Vector3 aVector) { return new Vector2(aVector.x, aVector.y); }
        public static Vector2 xz(this Vector3 aVector) { return new Vector2(aVector.x, aVector.z); }
        public static Vector2 yz(this Vector3 aVector) { return new Vector2(aVector.y, aVector.z); }
        public static Vector2 yx(this Vector3 aVector) { return new Vector2(aVector.y, aVector.x); }
        public static Vector2 zx(this Vector3 aVector) { return new Vector2(aVector.z, aVector.x); }
        public static Vector2 zy(this Vector3 aVector) { return new Vector2(aVector.z, aVector.y); }

        public static float GetVolumeCubic(this Bounds self) { return self.size.x * self.size.y * self.size.z; }
        public static float GetMaxArea2D(this Bounds self) { return Mathf.Max(Mathf.Max(self.size.x * self.size.y, self.size.y * self.size.z), self.size.x * self.size.z); }

        public static Color Opaque(this Color self) { return new Color(self.r, self.g, self.b, 1f); }

        public static void GizmosDrawPlane(Vector3 normal, Vector3 position, Color color, float size = 1f)
        {
            var v3 = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal, Vector3.forward)) < 0.999f ? Vector3.forward : Vector3.up).normalized * size;
            var corner0 = position + v3;
            var corner2 = position - v3;
            v3 = Quaternion.AngleAxis(90f, normal) * v3;
            var corner1 = position + v3;
            var corner3 = position - v3;

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = color;

            Gizmos.DrawLine(corner0, corner2);
            Gizmos.DrawLine(corner1, corner3);
            Gizmos.DrawLine(corner0, corner1);
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner0);
            //Gizmos.color = Color.red;
            //Gizmos.DrawRay(position, normal);
        }

        // Plane.Translate is not available in Unity 5
        public static Plane TranslateCustom(this Plane plane, Vector3 translation)
        {
            plane.distance += Vector3.Dot(translation.normalized, plane.normal) * translation.magnitude;
            return plane;
        }
    }

}

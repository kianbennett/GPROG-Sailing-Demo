using UnityEngine;

public struct TriangleData {
    //The corners of the triangle in global coordinates
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;

    public Vector3 center;
    public Vector3 normal;
    public float area;
    public float distToWater;
    public Vector3 velocity, velocityDir;
    public float cosTheta;

    public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3, Rigidbody rigidbody) {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;

        center = (p1 + p2 + p3) / 3f;
        normal = Vector3.Cross(p2 - p1, p3 - p1).normalized;
        area = GetTriangleArea(p1, p2, p3);

        velocity = GetTriangleVelocity(rigidbody, center);
        velocityDir = velocity.normalized;

        cosTheta = Vector3.Dot(velocityDir, normal);

        distToWater = 0;
    }

    public static float GetTriangleArea(Vector3 p1, Vector3 p2, Vector3 p3) {
        float a = Vector3.Distance(p1, p2);
        float c = Vector3.Distance(p3, p1);
        float area = (a * c * Mathf.Sin(Vector3.Angle(p2 - p1, p3 - p1) * Mathf.Deg2Rad)) / 2f;
        return area;
    }

    public static Vector3 GetTriangleVelocity(Rigidbody rigidbody, Vector3 center) {
        return rigidbody.velocity + Vector3.Cross(rigidbody.angularVelocity, center - rigidbody.worldCenterOfMass);
    }
}
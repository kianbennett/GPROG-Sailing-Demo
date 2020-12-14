using UnityEngine;

public static class MathUtil {

    // From https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/HandleUtility.cs#L115-L134
    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd) {
        Vector3 relativePoint = point - lineStart;
        Vector3 lineDirection = lineEnd - lineStart;
        float length = lineDirection.magnitude;
        Vector3 normalizedLineDirection = lineDirection;
        if (length > 0.000001f)
            normalizedLineDirection /= length;

        float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
        dot = Mathf.Clamp(dot, 0.0f, length);

        return lineStart + normalizedLineDirection * dot;
    }

    // Calculate distance between a point and a line.
    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd) {
        return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
    }

    // Möller-Trumbore algorithm from http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/pubs/raytri_tam.pdf
    public static bool IntersectsTriangle(Ray ray, Vector3 p0, Vector3 p1, Vector3 p2, out float t/*, out float u, out float v*/) {
        // Distance along the ray to the hit point
        t = 0; 
        // Two of the three barycentric coordinates that specify the location of the hit point on the triangle
        // (add out parameters if these are needed)
        float u = 0;
        float v = 0;

        // Vectors for 2 edges sharing p0
        Vector3 edge1 = p1 - p0;
        Vector3 edge2 = p2 - p0;

        // Calculate determinant
        Vector3 pvec = Vector3.Cross(ray.direction, edge2);
        float det = Vector3.Dot(edge1, pvec);

        // If determinant is close to zero, does not intersect
        if(det < Mathf.Epsilon && det > -Mathf.Epsilon) {
            return false;
        }

        // Calculate U parameter and test bounds
        Vector3 tvec = ray.origin - p0;

        u = Vector3.Dot(tvec, pvec);
        if(u < 0.0f || u > det) {
            return false;
        }

        // Calculate V parameter and test bounds
        Vector3 qvec = Vector3.Cross(tvec, edge1);

        v = Vector3.Dot(ray.direction, qvec);
        if(v < 0.0f || u + v > det) {
            return false;
        }

        // Calculate T, scale paramters, ray intersects triangle
        t = Vector3.Dot(edge2, qvec);
        float invDet = 1.0f / det;
        t *= invDet;
        u *= invDet;
        v *= invDet;

        return true;
    }
}
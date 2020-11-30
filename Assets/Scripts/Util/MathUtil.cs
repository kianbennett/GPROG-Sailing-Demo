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
}
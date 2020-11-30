using UnityEngine;

public static class FalloffGenerator {

    // Generates a circular falloff gradient
    public static float[,] GenerateFalloffMap(int width, int height, float a, float b) {
        float[,] map = new float[width, height];

        for(int i = 0; i < width; i++) {
            for(int j = 0; j < height; j++) {
                float distToCentre = 0;

                if(width > height) {
                    distToCentre = MathUtil.DistancePointLine(new Vector3(i, j), new Vector3(height / 2, height / 2), new Vector3(width - height / 2, height / 2));
                    distToCentre /= height * Mathf.Sqrt(0.5f); // Normalise
                } else if(height > width) {
                    distToCentre = MathUtil.DistancePointLine(new Vector3(i, j), new Vector3(width / 2, width / 2), new Vector3(width / 2, height - width / 2));
                    distToCentre /= width * Mathf.Sqrt(0.5f); // Normalise
                } else {
                    float x = i / (float) width; // Normalise coordinates
    				float y = j / (float) height;
                    distToCentre = Vector2.Distance(new Vector2(x, y), Vector2.one * 0.5f) / Mathf.Sqrt(0.5f);
                }

				map[i, j] = evaluate(distToCentre, a, b);
            }
        }

        return map;
    }

    // Arbitrary falloff function (google equation to visualise graph)
    private static float evaluate(float value, float a, float b) {
		return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
	}
}

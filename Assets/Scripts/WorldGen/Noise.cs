using UnityEngine;

[System.Serializable]
public class NoiseParameters {
    
    public float noiseScale;
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
}

public static class Noise {

    // Returns a perlin noise map
    public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {
        float[,] noiseMap = new float[width, height];
        
        // Create pseudo random number generator from seed
        System.Random r = new System.Random(seed);
        // Sample each octave from different location
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0 ; i < octaves; i++) {
            float offsetX = r.Next(-100000, 100000) + offset.x;
            float offsetY = r.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Avoid zero division errors by clamping to a positive value
        if(scale <= 0) scale = Mathf.Epsilon;

        // Keep track of range the values are in to be able to remap them to 0-1
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // For each point in the map get the noise value and increment it for each octave
        for(int i = 0; i < width; i++) {
            for(int j = 0; j < height; j++) {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int o = 0; o < octaves; o++) {
                    float sampleX = (i - width / 2f) / scale * frequency + octaveOffsets[o].x;
                    float sampleY = (j - height / 2f) / scale * frequency + octaveOffsets[o].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Clamp noise value
                if(noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if(noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;

                noiseMap[i, j] = noiseHeight;
            }
        }

        // Remap values to 0-1
        for(int i = 0; i < width; i++) {
            for(int j = 0; j < height; j++) {
                // Inverse lerp returns the percentage of value between start and end, giving a value between 0 and 1
                noiseMap[i, j] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[i, j]);
            }
        }

        return noiseMap;
    }
}

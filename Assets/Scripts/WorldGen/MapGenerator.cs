using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The terrain mesh's vertex colours are set depending in which terrain region the vertex lies
[System.Serializable]
public struct TerrainRegion {
    public string name;
    [Range(0, 1)]
    public float height, blend;
    public Color colour;
}

[System.Serializable]
public class TerrainObject {
    public IslandObject prefab;
    [Range(0, 1)]
    public float startHeight, endHeight, noiseThreshold;
    public bool alignToNormal;
    public NoiseParameters noise;
}

public class MapGenerator : Singleton<MapGenerator> {

    public int mapWidth, mapHeight;
    public int mapBuffer; // Min distance between edge of map and nearest island
    public bool autoUpdate;

    [Header("Island Parameters")]
    public int islandCount;
    public int minIslandSize, maxIslandSize;
    public Island islandPrefab;
    public Transform islandContainer;
    public int seed;
    public NoiseParameters islandNoise;

    [Header("Mesh Parameters")]
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public float uniformScale;
    public Vector2 falloffParameters;    

    [Header("Regions")]
    public TerrainRegion[] regions;

    [Header("Objects")]
    public TerrainObject[] terrainObjects;

    private List<Island> islands;

    protected override void Awake() {
        base.Awake();
        GenerateMap();

        // InvokeRepeating("NextIsland", 0.7f, 0.7f);
    }

    public void NextIsland() {
        seed++;
        GenerateMap();
    }

    public void GenerateMap() {
        ClearMap();

        Random.InitState(seed);

        islands = new List<Island>();
        for(int c = 0; c < islandCount; c++) {
            Vector2Int size = new Vector2Int(Random.Range(minIslandSize, maxIslandSize), Random.Range(minIslandSize, maxIslandSize));
            Vector2Int origin = getNextPointInArea(islands.Select(o => o.origin).ToArray(), size);
            origin = new Vector2Int(mapWidth / 2, mapHeight / 2);
            size = new Vector2Int(mapWidth, mapHeight);

            float[,] noiseMap = Noise.GenerateNoiseMap(size.x, size.y, Random.Range(0, int.MaxValue), 
                islandNoise.noiseScale, islandNoise.octaves, islandNoise.persistance, islandNoise.lacunarity, islandNoise.offset);
            float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(size.x, size.y, falloffParameters.x, falloffParameters.y);

            // Subtract falloff from noise map
            for(int i = 0; i < size.x; i++) {
                for(int j = 0; j < size.y; j++) {
                    noiseMap[i, j] = Mathf.Clamp01(noiseMap[i, j] - falloffMap[i, j]);
                }
            }

            Vector3 pos = new Vector3(-mapWidth / 2f + origin.x - size.x / 2f, 0, -mapHeight / 2f + origin.y - size.y / 2f);
            Island island = Instantiate(islandPrefab, pos, Quaternion.identity, islandContainer);
            island.origin = origin;
            island.size = size;
            island.GenerateIsland(noiseMap, meshHeightMultiplier, meshHeightCurve, uniformScale, regions);
            islands.Add(island);
        }
        
        GenerateObjects();

        Debug.Log("Generated map");
    }

    public void GenerateObjects() {
        foreach(Island island in islands) {
            // island.GenerateObjects(palmTreeInfo.prefab, palmTreeInfo.startHeight, palmTreeInfo.endHeight, palmTreeInfo.noiseThreshold, palmTreeNoise);
            // island.GenerateObjects(palmTreeInfo);
            // island.GenerateObjects(rockInfo);
            foreach(TerrainObject terrainObect in terrainObjects) {
                island.GenerateObjects(terrainObect);
            }
        }
    }

    public void ClearMap() {
        if(islands != null) {
            foreach(Island island in islands) {
                if(island) DestroyImmediate(island.gameObject);
            }
            islands.Clear();
        }
        foreach(Transform t in islandContainer) DestroyImmediate(t.gameObject);
    }

    // Use Mitchell's best candidate algorithm to distribute points evenly across a surface
    // For each point generate a set of samples and use the furthest away from any existing point
    private Vector2Int getNextPointInArea(Vector2Int[] existing, Vector2Int size) {
        Vector2Int bestCandidate = default;
        float bestDistance = 0;
        int sampleCount = 10; // High count yields better distrubition but is slower
        for(int i = 0; i < sampleCount; i++) {
            int x = Random.Range(size.x / 2 + mapBuffer, mapWidth - size.x / 2 - mapBuffer);
            int y = Random.Range(size.y / 2 + mapBuffer, mapHeight - size.y / 2 - mapBuffer);
            Vector2Int candidate = new Vector2Int(x, y);
            float distance = distToClosest(candidate, existing);
            if(distance > bestDistance) {
                bestCandidate = candidate;
                bestDistance = distance;
            }
        }
        return bestCandidate;
    }

    // Gets the distance from a new point to the nearest existing point in a set
    private float distToClosest(Vector2Int point, Vector2Int[] existing) {
        float dist = 0;
        foreach(Vector2Int p in existing) {
            float d = Vector2Int.Distance(point, p);
            if(dist == 0 || d < dist) dist = d;
        }
        return dist;
    }

    // Regenerate the map when values are changed
    void OnValuesUpdated() {
		if (!Application.isPlaying) {
            UnityEditor.EditorApplication.update -= OnValuesUpdated;
			GenerateMap();
		}
	}

    // Ensure values can't be edited outside of the valid range
    void OnValidate() {
        if(mapWidth < 1) mapWidth = 1;
        if(mapHeight < 1) mapHeight = 1;

        if(autoUpdate) {
            UnityEditor.EditorApplication.update += OnValuesUpdated;
        }
    }
}
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

[System.Serializable]
public class IslandParameters {
    public int seed;
    public int x, y;
    public int width, height;
}

public class MapGenerator : Singleton<MapGenerator> {

    public bool autoUpdate;

    [Header("Island Parameters")]
    public IslandParameters[] islandParameters;
    public Island islandPrefab;
    public Transform islandContainer;
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

    public void NextIslands() {
        foreach(IslandParameters parameters in islandParameters) {
            parameters.seed++;
        }
        GenerateMap();
    }

    public void GenerateMap() {
        ClearMap();

        islands = new List<Island>();
        for(int c = 0; c < islandParameters.Length; c++) {
            IslandParameters parameters = islandParameters[c];
            Random.InitState(parameters.seed);
            float[,] noiseMap = Noise.GenerateNoiseMap(parameters.width, parameters.height, Random.Range(0, int.MaxValue), 
                islandNoise.noiseScale, islandNoise.octaves, islandNoise.persistance, islandNoise.lacunarity, islandNoise.offset);
            float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(parameters.width, parameters.height, falloffParameters.x, falloffParameters.y);

            // Subtract falloff from noise map
            for(int i = 0; i < parameters.width; i++) {
                for(int j = 0; j < parameters.height; j++) {
                    noiseMap[i, j] = Mathf.Clamp01(noiseMap[i, j] - falloffMap[i, j]);
                }
            }

            Vector3 pos = new Vector3(parameters.x, 0, parameters.y);
            Island island = Instantiate(islandPrefab, pos, Quaternion.identity, islandContainer);
            island.origin = new Vector2Int(parameters.x, parameters.y);
            island.size = new Vector2Int(parameters.width, parameters.height);
            island.GenerateIsland(noiseMap, meshHeightMultiplier, meshHeightCurve, uniformScale, regions);
            islands.Add(island);
        }
        
        GenerateObjects();

        // Debug.Log("Generated map");
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
        if(autoUpdate) {
            UnityEditor.EditorApplication.update += OnValuesUpdated;
        }
    }
}
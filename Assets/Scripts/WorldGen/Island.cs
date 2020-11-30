using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Island : MonoBehaviour {

    [ReadOnly] public Vector2Int origin, size;
    public IslandMesh mesh;
    public Transform objectContainer;

    private float[,] heightMap;
    private float heightMultiplier;
    private AnimationCurve heightCurve;
    private float uniformScale;

    void Update() {
        // for(int j = 0; j < MapGenerator.instance.mapHeight; j++) {
        //     for(int i = 0; i < MapGenerator.instance.mapWidth; i++) {
        //         float y = heightCurve.Evaluate(heightMap[i, j]) * heightMultiplier;
        //         Vector3 pos = new Vector3((MapGenerator.instance.mapWidth - 1) / -2f + i, y, (MapGenerator.instance.mapHeight - 1) / 2f - j) * uniformScale;
        //         pos += transform.position;
        //         RaycastHit hitInfo;
        //         bool hit = Physics.Raycast(pos + Vector3.up, Vector3.down, out hitInfo, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        //         if(hit) {
        //             Debug.DrawLine(pos + Vector3.up, hitInfo.point, Color.green);
        //             Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal * 0.2f, Color.blue);
        //         }
        //     }
        // }
    }

    public void GenerateIsland(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, float uniformScale, TerrainRegion[] terrainRegions) {
        this.heightMap = heightMap;
        this.heightMultiplier = heightMultiplier;
        this.heightCurve = heightCurve;
        this.uniformScale = uniformScale;
        mesh.UpdateMesh(heightMap, heightMultiplier, heightCurve, uniformScale, terrainRegions);    
    }

    public void GenerateObjects(TerrainObject objectInfo) {
        cleanUpObjects();

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        NoiseParameters noiseParameters = objectInfo.noise;

        float[,] noiseMap = Noise.GenerateNoiseMap(width, height, Random.Range(0, int.MaxValue),
            noiseParameters.noiseScale, noiseParameters.octaves, noiseParameters.persistance, noiseParameters.lacunarity, Vector2.zero);

        float startHeight = heightCurve.Evaluate(objectInfo.startHeight) * heightMultiplier;
        float endHeight = heightCurve.Evaluate(objectInfo.endHeight) * heightMultiplier;
        
        for(int j = 0; j < height; j++) {
            for(int i = 0; i < width; i++) {
                float y = heightCurve.Evaluate(heightMap[i, j]) * heightMultiplier;
                if(y >= startHeight && y <= endHeight && noiseMap[i, j] > objectInfo.noiseThreshold) {
                    Vector3 pos = new Vector3((width - 1) / -2f + i, y, (height - 1) / 2f - j) * uniformScale;
                    pos += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * 0.2f;
                    pos += transform.position;

                    IslandObject obj = Instantiate(objectInfo.prefab, pos, Quaternion.identity);
                    obj.transform.SetParent(objectContainer, true);
                    obj.Generate();

                    if(objectInfo.alignToNormal) {
                        Ray ray = new Ray(pos + Vector3.up * 10, Vector3.down);
                        RaycastHit hitInfo;
                        bool hit = mesh.meshCollider.Raycast(ray, out hitInfo, Mathf.Infinity);

                        if(hit) {
                            obj.transform.up = hitInfo.normal;
                        }
                    }
                }
            }
        }
    }

    private void cleanUpObjects() {
        foreach(Transform t in objectContainer) {
            DestroyImmediate(t.gameObject);
        }
    }
}

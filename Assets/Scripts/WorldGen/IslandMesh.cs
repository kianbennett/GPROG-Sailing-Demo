using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex;

    public MeshData(int meshWidth, int meshHeight, float scale) {
        vertices = new Vector3[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        uvs = new Vector2[meshWidth * meshHeight];

        // Unitialise mesh to flat plane
        int vertexIndex = 0;
        for(int j = 0; j < meshHeight; j++) {
            for(int i = 0; i < meshWidth; i++) {
                vertices[vertexIndex] = new Vector3((meshWidth - 1) / -2f + i, 0, (meshHeight - 1) / 2f - j) * scale;
                uvs[vertexIndex] = new Vector2(i / (float) meshWidth, j / (float) meshHeight);

                if(i < meshWidth - 1 && j < meshHeight - 1) {
                    AddTriangle(vertexIndex, vertexIndex + meshWidth + 1, vertexIndex + meshWidth);
                    AddTriangle(vertexIndex + meshWidth + 1, vertexIndex, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }
    }

    public void AddTriangle(int a, int b, int c) {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }
}

public class IslandMesh : MonoBehaviour {

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public void UpdateMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, float uniformScale, TerrainRegion[] terrainRegions) {
        MeshData meshData = generateMeshData(heightMap, heightMultiplier, heightCurve, uniformScale);

        meshFilter.sharedMesh = meshData.CreateMesh();
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // if(!Application.isPlaying) AssetDatabase.CreateAsset(meshRenderer.sharedMaterial, "Assets/Materials/Mat_Terrain.mat");
        // if(!Application.isPlaying) AssetDatabase.CreateAsset(meshFilter.sharedMesh, "Assets/Models/TerrainMesh.asset");

        UpdateMaterial(heightMultiplier, heightCurve, uniformScale, terrainRegions);
    }

    private MeshData generateMeshData(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, float uniformScale) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        MeshData meshData = new MeshData(width, height, uniformScale);
        int vertexIndex = 0;

        for(int j = 0; j < height; j++) {
            for(int i = 0; i < width; i++) {
                float y = heightCurve.Evaluate(heightMap[i, j]) * heightMultiplier;
                meshData.vertices[vertexIndex].y = y;
                vertexIndex++;
            }
        }

        return meshData;
    }

    // Passes height and colour data to the material shader so vertex colours can be set accordingly
    public void UpdateMaterial(float heightMultiplier, AnimationCurve heightCurve, float uniformScale, TerrainRegion[] terrainRegions) {
        meshRenderer.sharedMaterial.SetFloat("_MinHeight", heightCurve.Evaluate(0) * heightMultiplier * uniformScale);
        meshRenderer.sharedMaterial.SetFloat("_MaxHeight", heightCurve.Evaluate(1) * heightMultiplier * uniformScale);
        meshRenderer.sharedMaterial.SetFloat("_Regions", terrainRegions.Length);

        for(int i = 0; i < terrainRegions.Length; i++) {
            meshRenderer.sharedMaterial.SetColor("_Colour" + (i + 1), terrainRegions[i].colour);
            meshRenderer.sharedMaterial.SetFloat("_Height" + (i + 1), terrainRegions[i].height);
            meshRenderer.sharedMaterial.SetFloat("_Blend" + (i + 1), terrainRegions[i].blend);
        }
    }
}

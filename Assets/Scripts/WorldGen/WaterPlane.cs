using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlane : MonoBehaviour {

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public int width, height;
    public float resolution;

    public void GenerateMesh() {
        MeshData meshData = new MeshData(Mathf.CeilToInt(width * resolution) + 1, Mathf.CeilToInt(height * resolution) + 1, 1f / resolution);
        meshFilter.sharedMesh = meshData.CreateMesh();
    }
}

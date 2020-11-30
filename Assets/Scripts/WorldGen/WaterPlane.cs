using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlane : MonoBehaviour {

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public int width, height;
    public float resolution;

    public void GenerateMesh() {
        MeshData meshData = new MeshData((int) (width * resolution), (int) (height * resolution), 1f / resolution);
        meshFilter.sharedMesh = meshData.CreateMesh();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

//Help struct to store triangle data so we can sort the distances
public struct VertexData {
    //An index so we can form clockwise triangles
    public int index;
    public Vector3 position;
    public float waterDist;

    public VertexData(int index, Vector3 position, float waterDist) {
        this.index = index;
        this.position = position;
        this.waterDist = waterDist;
    }
}

//Data that belongs to one triangle in the body mesh, required to calculate slamming force
public class SlammingForceData  {
    //The area of the original triangles - calculate once in the beginning because always the same
    public float originalArea;
    //How much area of a triangle in the whole boat is submerged
    public float submergedArea;
    //Same as above but previous time step
    public float previousSubmergedArea;
    //Need to save the center of the triangle to calculate the velocity
    public Vector3 triangleCenter;
    public Vector3 velocity;
    //Same as above but previous time step
    public Vector3 previousVelocity;

    public SlammingForceData(float area) {
        originalArea = area;
    }
}

[ExecuteInEditMode]
public class ShipPhysics : MonoBehaviour {

    public new Rigidbody rigidbody;
    public MeshRenderer waterMesh;
    public MeshRenderer bodyMesh;
    public MeshFilter bodyMeshFilter;
    public float patchResolution;
    public bool visualisePatch, visualiseRays, visualiseForces;
    public float waterDensity;
    public float slammingForceAmount;

    [Header("Pressure Drag Parameters")]
    public float pressureFalloff = 0.5f;
    public float pressureCoeff1 = 10f;
    public float pressureCoeff2 = 10f;
    public float suctionFalloff = 0.5f;
    public float suctionCoeff1 = 10f;
    public float suctionCoeff2 = 10f;

    private MeshData patchMesh;
    private Vector2Int patchSize;
    private Vector3[] patchVertexOrigins;
    private Vector3 patchOrigin;
    private float[] bodyVertexDistances; // The heights above/below water for each vertex in the body mesh
    private List<TriangleData> underwaterTriangles = new List<TriangleData>();
    private List<int> underwaterTriangleIndices = new List<int>(); // Index of the original mesh triangle index for each new underwater triangle
    private List<SlammingForceData> slammingForceData;
    private float bodySurfaceArea;

    private int[] bodyMeshTriangles;
    private Vector3[] bodyMeshVertices;

    private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    // Wave paramters
    private float phase, gravity, depth;
    private Vector3[] dirs = new Vector3[4];
    private float[] speeds = new float[4];
    private float[] amplitudes = new float[4];

    void OnValidate() {
        createPatchMesh();

        bodyMeshTriangles = bodyMeshFilter.sharedMesh.triangles;
        bodyMeshVertices = bodyMeshFilter.sharedMesh.vertices;
        bodyVertexDistances = new float[bodyMeshVertices.Length];
        // Calculate each of the body triangle's area for the slamming force
        slammingForceData = new List<SlammingForceData>();
        for(int i = 0; i < bodyMeshTriangles.Length / 3; i++) {
            Vector3 p1 = bodyMeshVertices[bodyMeshTriangles[i * 3]];
            Vector3 p2 = bodyMeshVertices[bodyMeshTriangles[i * 3 + 1]];
            Vector3 p3 = bodyMeshVertices[bodyMeshTriangles[i * 3 + 2]];
            float area = TriangleData.GetTriangleArea(p1, p2, p3);
            slammingForceData.Add(new SlammingForceData(area));
            bodySurfaceArea += area;
        }
    }

    void FixedUpdate() {
        // Update patch position
        patchOrigin = bodyMesh.bounds.center;
        patchOrigin.y = waterMesh.transform.position.y;
        if(visualisePatch) drawPatchDebug();

        // Get properties from shader
        phase = waterMesh.sharedMaterial.GetFloat("_Phase");
        gravity = waterMesh.sharedMaterial.GetFloat("_Gravity");
        depth = waterMesh.sharedMaterial.GetFloat("_Depth");

        for(int i = 0; i < 4; i++) {
            dirs[i] = waterMesh.sharedMaterial.GetVector("_Direction" + (i + 1));
            speeds[i] = waterMesh.sharedMaterial.GetFloat("_Speed" + (i + 1));
            amplitudes[i] = waterMesh.sharedMaterial.GetFloat("_Amplitude" + (i + 1));
        }

        // stopwatch.Restart();
        updatePatchMesh();
        // Debug.Log("Update patch mesh: " + stopwatch.ElapsedTicks);
        // stopwatch.Restart();
        updateVertexDistances();
        // Debug.Log("Update vertex distances: " + stopwatch.ElapsedTicks);
        // stopwatch.Restart();
        calculateUnderwaterTriangles();
        // Debug.Log("Calculate underwater triangles: " + stopwatch.ElapsedTicks);
        // stopwatch.Restart();
        addTriangleForces();
        // Debug.Log("Add triangle forces: " + stopwatch.ElapsedTicks);

        // Draw debug lines for the force applied to each triangle
        if(visualiseForces) {
            foreach(TriangleData triangle in underwaterTriangles) {
                Debug.DrawLine(triangle.center, triangle.center + triangle.normal, Color.blue);
                Debug.DrawLine(triangle.p1, triangle.p2, Color.cyan);
                Debug.DrawLine(triangle.p2, triangle.p3, Color.cyan);
                Debug.DrawLine(triangle.p3, triangle.p1, Color.cyan);
            }
        }

        // stopwatch.Stop();
        // Debug.Log("Elapsed: " + stopwatch.ElapsedTicks);
    }

    // Create the mesh underneath the ship with Gerstner offset, so we can raycast down to get the height
    private void createPatchMesh() {
        // Get the maximum offset the gerstner formula can produce
        Vector3 maxOffset = Vector3.zero;
        for(int i = 0; i < 4; i++) {
            Vector3 offset = maxGerstnerOffset(waterMesh.sharedMaterial.GetFloat("_Depth"), 
                                waterMesh.sharedMaterial.GetVector("_Direction" + (i + 1)),
                                waterMesh.sharedMaterial.GetFloat("_Amplitude" + (i + 1)));
            maxOffset += new Vector3(Mathf.Abs(offset.x), 0, Mathf.Abs(offset.z));
        }

        // Get maximum size of patch based on mesh bounds and max gerstner offset
        Vector3 bodySize = bodyMesh.bounds.size;
        patchSize = new Vector2Int(Mathf.CeilToInt((bodySize.x + maxOffset.x * 2) * patchResolution) + 1, Mathf.CeilToInt((bodySize.z + maxOffset.z * 2) * patchResolution) + 1);

        // Build patch mesh and store original vertex positions
        patchMesh = new MeshData(patchSize.x, patchSize.y, 1f / patchResolution);
        patchVertexOrigins = new Vector3[patchMesh.vertices.Length];
        Array.Copy(patchMesh.vertices, patchVertexOrigins, patchMesh.vertices.Length);

        // Debug.Log("Calculated patch mesh");
    }

    // Update the vertex positions in the patch
    private void updatePatchMesh() {
        for(int i = 0; i < patchMesh.vertices.Length; i++) {
            Vector3 gerstnerOffset = Vector3.zero;
            for(int w = 0; w < 4; w++) {
                gerstnerOffset += gerstnerWaveOffset(patchOrigin + patchVertexOrigins[i], 
                    phase, Time.time * speeds[w], gravity, depth, dirs[w], amplitudes[w]);
            }
            patchMesh.vertices[i] = patchVertexOrigins[i] + gerstnerOffset;
        }
    }

    // Draw Debug lines between the patch vertices
    private void drawPatchDebug() {
        for(int i = 0; i < patchMesh.triangles.Length / 3; i++) {
            Vector3 v1 = patchMesh.vertices[patchMesh.triangles[i * 3]];
            Vector3 v2 = patchMesh.vertices[patchMesh.triangles[i * 3 + 1]];
            Vector3 v3 = patchMesh.vertices[patchMesh.triangles[i * 3 + 2]];
            Debug.DrawLine(patchOrigin + v1, patchOrigin + v2, Color.white);
            Debug.DrawLine(patchOrigin + v2, patchOrigin + v3, Color.white);
            Debug.DrawLine(patchOrigin + v3, patchOrigin + v1, Color.white);
        }
    }

    // Gets the height above/below the water line for each vertex in the body mesh
    private void updateVertexDistances() {
        for(int i = 0; i < bodyMeshVertices.Length; i++) {
            // Transform the mesh vertices to world space
            Vector3 vertex = bodyMesh.transform.TransformPoint(bodyMeshVertices[i]);

            Ray ray = new Ray(vertex, Vector3.down);
            float dist;
            // bool hit = intersectsMesh(ray, patchMesh, patchOrigin, out dist);
            bool hit = patchMesh.RayIntersects(ray, patchOrigin, out dist);

            if(hit && visualiseRays) {
                Debug.DrawLine(vertex, vertex + Vector3.down * dist, Color.green);
            }

            bodyVertexDistances[i] = hit ? dist : 0;
        }
    }

    private void addTriangleForces() {
        float underwaterArea = 0;
        float underwaterMinZ = 0, underwaterMaxZ = 0;
        foreach(TriangleData triangle in underwaterTriangles) {
            if(triangle.distToWater < 0) {
                underwaterArea += triangle.area;
            }

            float z1 = (Quaternion.Inverse(transform.rotation) * (triangle.p1 - bodyMesh.transform.position)).z;
            float z2 = (Quaternion.Inverse(transform.rotation) * (triangle.p2 - bodyMesh.transform.position)).z;
            float z3 = (Quaternion.Inverse(transform.rotation) * (triangle.p3 - bodyMesh.transform.position)).z;
            if(z1 < underwaterMinZ || underwaterMinZ == 0) underwaterMinZ = z1;
            if(z2 < underwaterMinZ || underwaterMinZ == 0) underwaterMinZ = z2;
            if(z3 < underwaterMinZ || underwaterMinZ == 0) underwaterMinZ = z3;
            if(z1 > underwaterMaxZ || underwaterMaxZ == 0) underwaterMaxZ = z1;
            if(z2 > underwaterMaxZ || underwaterMaxZ == 0) underwaterMaxZ = z2;
            if(z3 > underwaterMaxZ || underwaterMaxZ == 0) underwaterMaxZ = z3;
        }

        float underwaterLength = underwaterMaxZ - underwaterMinZ;
        float resistanceCoeff = ShipForces.ResistanceCoefficient(rigidbody.velocity.magnitude, underwaterLength);

        calculateSlammingVelocities();

        for(int i = 0; i < underwaterTriangles.Count; i++) {
            TriangleData triangle = underwaterTriangles[i];

            Vector3 buoyancyForce = ShipForces.CalculateBuoyancyForce(triangle, waterDensity);
            Vector3 viscousWaterResistance = ShipForces.CalculateViscousWaterResistance(triangle, waterDensity, resistanceCoeff);
            Vector3 pressureDrag = ShipForces.CalculatePressureDrag(triangle, pressureFalloff, pressureCoeff1, pressureCoeff2, suctionFalloff, suctionCoeff1, suctionCoeff2);

            int originalTriangleIndex = underwaterTriangleIndices[i];
            SlammingForceData slammingData = slammingForceData[originalTriangleIndex];
            Vector3 slammingForce = ShipForces.CalculateSlammingForce(triangle, slammingData, slammingForceAmount, bodySurfaceArea, rigidbody.mass);

            Vector3 totalForce = buoyancyForce + viscousWaterResistance + pressureDrag + slammingForce;

            rigidbody.AddForceAtPosition(totalForce, triangle.center);
        }
    }

    private Vector3 gerstnerWaveOffset(Vector3 pos, float phase, float time, float gravity, float depth, Vector3 dir, float amplitude) {
        float dirLength = dir.magnitude;
        float angularFreq = Mathf.Sqrt(gravity * dirLength * (float) Math.Tanh(dirLength * depth));
        float theta = dir.x * pos.x +  dir.z * pos.z - angularFreq * time - phase;
        float x = -(dir.x / dirLength) * (amplitude / (float) Math.Tanh(dirLength * depth)) * Mathf.Sin(theta);
        float y = amplitude * Mathf.Cos(theta);
        float z = -(dir.z / dirLength) * (amplitude / (float) Math.Tanh(dirLength * depth)) * Mathf.Sin(theta);
        return new Vector3(x, y, z);
    }

    private Vector3 maxGerstnerOffset(float depth, Vector3 dir, float amplitude) {
        float dirLength = dir.magnitude;
        float x = (dir.x / dirLength) * (amplitude / (float) Math.Tanh(dirLength * depth));
        float z = (dir.z / dirLength) * (amplitude / (float) Math.Tanh(dirLength * depth));
        return new Vector3(x, 0, z);
    }

    private float getHeightAboveWater(Vector3 pos) {
        Ray ray = new Ray(pos, Vector3.down);
        float dist;
        bool hit = patchMesh.RayIntersects(ray, patchOrigin, out dist);
        return hit ? dist : 0;
    }

    // Calculate the current velocity at the center of each triangle of the original boat mesh
    private void calculateSlammingVelocities() {
        for (int i = 0; i < slammingForceData.Count; i++) {
            //Set the new velocity to the old velocity
            slammingForceData[i].previousVelocity = slammingForceData[i].velocity;
            //Center of the triangle in world space
            Vector3 center = transform.TransformPoint(slammingForceData[i].triangleCenter);
            //Get the current velocity at the center of the triangle
            slammingForceData[i].velocity = TriangleData.GetTriangleVelocity(rigidbody, center);
        }
    }

    private void calculateUnderwaterTriangles() {
        underwaterTriangles.Clear();
        underwaterTriangleIndices.Clear();

        // Switch the submerged triangle area with the one in the previous time step
        for (int i = 0; i < slammingForceData.Count; i++) {
            slammingForceData[i].previousSubmergedArea = slammingForceData[i].submergedArea;
        }

        VertexData[] triangleVertices = new VertexData[3];

        for(int i = 0; i < bodyMeshTriangles.Length / 3; i++) {
            int i1 = bodyMeshTriangles[i * 3];
            int i2 = bodyMeshTriangles[i * 3 + 1];
            int i3 = bodyMeshTriangles[i * 3 + 2];

            // All vertices are above the water
            if(bodyVertexDistances[i1] > 0f && bodyVertexDistances[i2] > 0f && bodyVertexDistances[i3] > 0f) {
                slammingForceData[i].submergedArea = 0f;
                continue;
            }

            // Store these vertices in an array as they need sorting
            triangleVertices[0] = new VertexData(0, bodyMesh.transform.TransformPoint(bodyMeshVertices[i1]), bodyVertexDistances[i1]);
            triangleVertices[1] = new VertexData(1, bodyMesh.transform.TransformPoint(bodyMeshVertices[i2]), bodyVertexDistances[i2]);
            triangleVertices[2] = new VertexData(2, bodyMesh.transform.TransformPoint(bodyMeshVertices[i3]), bodyVertexDistances[i3]);

            // All vertices are underwater
            if(triangleVertices[0].waterDist < 0f && triangleVertices[1].waterDist < 0f && triangleVertices[2].waterDist < 0f) {
                TriangleData triangleData = new TriangleData(triangleVertices[0].position, triangleVertices[1].position, triangleVertices[2].position, rigidbody);
                triangleData.distToWater = getHeightAboveWater(triangleData.center);
                underwaterTriangles.Add(triangleData);
                underwaterTriangleIndices.Add(i);
                slammingForceData[i].submergedArea = slammingForceData[i].originalArea;
            } 
            // 1 or 2 vertices underwater
            else {
                // Ordering the vertices by height above water will ensure any vertices above the water will be first in the array
                triangleVertices = triangleVertices.OrderByDescending(o => o.waterDist).ToArray();

                // One vertex above the water
                if(triangleVertices[0].waterDist > 0f && triangleVertices[1].waterDist < 0f && triangleVertices[2].waterDist < 0f) {
                    float area = addTrianglesOneAboveWater(triangleVertices);
                    // Two triangles are being added and they both share the same original triangle
                    underwaterTriangleIndices.Add(i);
                    underwaterTriangleIndices.Add(i);
                    slammingForceData[i].submergedArea = area;
                } 
                // Two vertices above the water
                else if(triangleVertices[0].waterDist > 0f && triangleVertices[1].waterDist > 0f && triangleVertices[2].waterDist < 0f) {
                    float area = addTrianglesTwoAboveWater(triangleVertices);
                    underwaterTriangleIndices.Add(i);
                    slammingForceData[i].submergedArea = area;
                }
            }
        }
    }

    //Build the new triangles where one of the vertices is above the water, return submerged area
    private float addTrianglesOneAboveWater(VertexData[] vertices) {
        //H is always at position 0
        Vector3 H = vertices[0].position;

        //Left of H is M. Right of H is L

        //Find the index of M
        int M_index = vertices[0].index - 1;
        if (M_index < 0) {
            M_index = 2;
        }

        //We also need the heights to water
        float h_H = vertices[0].waterDist;
        float h_M = 0f;
        float h_L = 0f;

        Vector3 M = Vector3.zero;
        Vector3 L = Vector3.zero;

        //This means M is at position 1 in the List
        if (vertices[1].index == M_index) {
            M = vertices[1].position;
            L = vertices[2].position;

            h_M = vertices[1].waterDist;
            h_L = vertices[2].waterDist;
        } else {
            M = vertices[2].position;
            L = vertices[1].position;

            h_M = vertices[2].waterDist;
            h_L = vertices[1].waterDist;
        }
        
        //Now we can calculate where we should cut the triangle to form 2 new triangles
        //because the resulting area will always form a square

        //Point I_M
        Vector3 MH = H - M;
        float t_M = -h_M / (h_H - h_M);
        Vector3 MI_M = t_M * MH;
        Vector3 I_M = MI_M + M;

        //Point I_L
        Vector3 LH = H - L;
        float t_L = -h_L / (h_H - h_L);
        Vector3 LI_L = t_L * LH;
        Vector3 I_L = LI_L + L;

        //Save the data of the 2 triangles below the water  
        TriangleData t1 = new TriangleData(M, I_M, I_L, rigidbody);
        TriangleData t2 = new TriangleData(M, I_L, L, rigidbody);
        t1.distToWater = getHeightAboveWater(t1.center);
        t2.distToWater = getHeightAboveWater(t2.center);
        underwaterTriangles.Add(t1);
        underwaterTriangles.Add(t2);

        return t1.area + t2.area;
    }

    //Build the new triangles where two of the vertices are above the water, return submerged area
    private float addTrianglesTwoAboveWater(VertexData[] vertices) {
        //H and M are above the water
        //H is after the vertice that's below water, which is L
        //So we know which one is L because it is last in the sorted list
        Vector3 L = vertices[2].position;

        //Find the index of H
        int H_index = vertices[2].index + 1;
        if (H_index > 2) {
            H_index = 0;
        }

        //We also need the heights to water
        float h_L = vertices[2].waterDist;
        float h_H = 0f;
        float h_M = 0f;

        Vector3 H = Vector3.zero;
        Vector3 M = Vector3.zero;

        //This means that H is at position 1 in the list
        if (vertices[1].index == H_index) {
            H = vertices[1].position;
            M = vertices[0].position;

            h_H = vertices[1].waterDist;
            h_M = vertices[0].waterDist;
        } else {
            H = vertices[0].position;
            M = vertices[1].position;

            h_H = vertices[0].waterDist;
            h_M = vertices[1].waterDist;
        }

        //Now we can find where to cut the triangle

        //Point J_M
        Vector3 LM = M - L;
        float t_M = -h_L / (h_M - h_L);
        Vector3 LJ_M = t_M * LM;
        Vector3 J_M = LJ_M + L;

        //Point J_H
        Vector3 LH = H - L;
        float t_H = -h_L / (h_H - h_L);
        Vector3 LJ_H = t_H * LH;
        Vector3 J_H = LJ_H + L;

        //Save the data of the 1 triangle below the water
        TriangleData t = new TriangleData(L, J_H, J_M, rigidbody);
        t.distToWater = getHeightAboveWater(t.center);
        underwaterTriangles.Add(t);

        return t.area;
    }
}

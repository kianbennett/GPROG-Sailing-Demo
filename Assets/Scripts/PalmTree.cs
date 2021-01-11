using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class PalmTree : IslandObject {
    
    public Transform componentContainer;
    public Transform leavesContainer;

    [Header("Components")]
    public GameObject trunkSegment;
    public GameObject trunkSegmentTop;
    public GameObject[] leafObjects;
    public GameObject coconutObject;
    public Material smallLeafMaterial;

    [Header("Parameters")]
    public bool autoGenerate;
    public int seed;
    [Range(0.01f, 8)]
    public float length;
    [Range(0.01f, .8f)]
    public float curveAmount;
    [Range(0.01f, 3f)]
    public float leafDensity;
    public float trunkWidth;

    public void RandomiseValues() {
        seed = Random.Range(0, int.MaxValue);
        length = Random.Range(1.2f, 7f);
        curveAmount = Random.Range(0.01f, 0.45f);
        transform.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        leafDensity = Random.Range(1.0f, 1.4f);
        trunkWidth = Random.Range(0.6f, 1.4f);
    }

    public override void Generate() {
        cleanUp();
        RandomiseValues();

        float segmentLength = 0.45f;
        float segments = Mathf.FloorToInt(length / segmentLength);
        float scaledSegmentLength = length / segments;

        Vector3 lastSegmentPos = Vector3.zero;
        float segmentAngle = 0;

        for(int i = 0; i < segments; i++) {
            float y = lastSegmentPos.y + scaledSegmentLength * Mathf.Cos(segmentAngle * Mathf.Deg2Rad);
            float x = Mathf.Pow(curveAmount * y, 2);
            Vector3 pos = new Vector3(x, y);
            segmentAngle = Vector3.Angle(Vector3.up, pos - lastSegmentPos);

            GameObject segment = Instantiate(i < segments - 1 ? trunkSegment : trunkSegmentTop, lastSegmentPos, Quaternion.Euler(0, 0, -segmentAngle));
            segment.transform.localScale = new Vector3(trunkWidth, scaledSegmentLength / segmentLength, trunkWidth);
            segment.transform.SetParent(componentContainer, false);

            lastSegmentPos = pos;
        }

        leavesContainer.transform.localPosition = lastSegmentPos;
        leavesContainer.transform.localRotation = Quaternion.Euler(0, 0, -segmentAngle + 60 * curveAmount);

        float leavesScale = Mathf.Clamp(0.5f + length / 3, 0.5f, 2f);
        leavesContainer.transform.localScale = Vector3.one * leavesScale;

        System.Random random = new System.Random(seed);

        float bigLeavesAmount = (4 + 6 * (float) random.NextDouble()) * leafDensity;
        float smallLeavesAmount = (4 + 6 * (float) random.NextDouble()) * leafDensity * 0.66f;
        float leavesAmountCurrent = 0;
        int coconutCount = Mathf.Clamp(random.Next(0, (int) (4 * leafDensity)), 0, 6);

        // leaf size, angle
        List<Tuple<int, float>> leaves = new List<Tuple<int, float>>();
        List<Tuple<int, float>> smallLeaves = new List<Tuple<int, float>>();
        List<Tuple<int, float>> coconuts = new List<Tuple<int, float>>();

        while(leavesAmountCurrent < bigLeavesAmount) {
            int leaf = random.Next(0, leafObjects.Length);
            leavesAmountCurrent += leaf + 1;
            leaves.Add(new Tuple<int, float>(leaf, getNextLeafAngle(leaves, random)));
        }

        leavesAmountCurrent = 0;
        while(leavesAmountCurrent < smallLeavesAmount) {
            int leaf = random.Next(0, leafObjects.Length - 1); // Don't use biggest leaf object for small leaves
            leavesAmountCurrent += leaf + 1;
            smallLeaves.Add(new Tuple<int, float>(leaf, getNextLeafAngle(smallLeaves, random)));
        }

        foreach(Tuple<int, float> leaf in leaves) {
            GameObject leafObject = Instantiate(leafObjects[leaf.Item1], Vector3.zero, Quaternion.identity);
            leafObject.transform.SetParent(leavesContainer, false);
            float xRot = -10 + 20 * (float) random.NextDouble();
            leafObject.transform.localRotation = Quaternion.Euler(new Vector3(xRot, leaf.Item2, 0));
            leafObject.transform.Translate(leafObject.transform.forward * -0.05f, Space.World);
        }

        foreach(Tuple<int, float> leaf in smallLeaves) {
            GameObject leafObject = Instantiate(leafObjects[leaf.Item1], Vector3.zero, Quaternion.identity);
            leafObject.transform.SetParent(leavesContainer, false);
            float xRot = -20 + 15 * (float) random.NextDouble();
            leafObject.transform.localRotation = Quaternion.Euler(new Vector3(xRot, leaf.Item2, 0));
            leafObject.transform.Translate(leafObject.transform.forward * -0.1f, Space.World);
            leafObject.GetComponent<Renderer>().sharedMaterial = smallLeafMaterial;
            leafObject.transform.localScale *= 0.5f;
        }

        for(int i = 0; i < coconutCount; i++) {
            float angle = getNextLeafAngle(coconuts, random);
            coconuts.Add(new Tuple<int, float>(0, angle));

            GameObject coconut = Instantiate(coconutObject, Vector3.zero, Quaternion.identity);
            coconut.transform.SetParent(leavesContainer, false);
            coconut.transform.localRotation = Quaternion.Euler(new Vector3(-35f, angle, 0));
            coconut.transform.Translate(Vector3.forward * 0.15f + Vector3.up * -0.2f);
            coconut.transform.localScale = Vector3.one * (0.7f + 0.6f * (float) random.NextDouble());
        }
    }

    // Mitchell's best candidate
    private float getNextLeafAngle(List<Tuple<int, float>> existing, System.Random random) {
        float bestCandidate = 0;
        float bestDistance = 0;
        for(int i = 0; i < 10; i++) {
            float candidate = 360 * (float) random.NextDouble();

            // Take the first candidate if it's the first leaf
            if(existing.Count == 0) {
                bestCandidate = candidate;
                break;
            }

            // Get distance to nearest leaf
            float distance = 0;
            foreach(Tuple<int, float> l in existing) {
                float d = Mathf.Abs(candidate - l.Item2);
                // Debug.Log("d: " + d);
                if(distance == 0 || d < distance) {
                    distance = d;
                }
            }
            // Debug.Log(candidate + ", " + distance + ", " + bestDistance);
            if(distance > bestDistance) {
                bestCandidate = candidate;
                bestDistance = distance;
            }
        }

        return bestCandidate;
    }

    private void cleanUp() {
        for (int i = componentContainer.childCount - 1; i >= 0; i--) {
            DestroyImmediate(componentContainer.transform.GetChild(i).gameObject);
        }
        for (int i = leavesContainer.childCount - 1; i >= 0; i--) {
            DestroyImmediate(leavesContainer.transform.GetChild(i).gameObject);
        }
    }

    void OnValuesUpdated() {
		if (!Application.isPlaying) {
            UnityEditor.EditorApplication.update -= OnValuesUpdated;
			Generate();
		}
	}

    void OnValidate() {
        if(length < 0.5f) length = 0.5f;

        if(autoGenerate) {
            UnityEditor.EditorApplication.update += OnValuesUpdated;
        }
    }
}

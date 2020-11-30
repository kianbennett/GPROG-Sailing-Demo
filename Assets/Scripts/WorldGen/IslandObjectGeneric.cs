using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandObjectGeneric : IslandObject {

    public GameObject[] objects;
    public float minScale = 0.75f;
    public float maxScale = 1.25f;

    public override void Generate() {
        base.Generate();

        int obj = Random.Range(0, objects.Length);
        for(int i = 0; i < objects.Length; i++) {
            objects[i].SetActive(obj == i);
        }

        transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
        transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
    }
}

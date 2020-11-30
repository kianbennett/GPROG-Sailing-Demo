using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainRaycastTest : MonoBehaviour {

    public bool boolean;

    void Update() {
        
    }

    private void cast() {
        RaycastHit hitInfo;
        bool hit = Physics.Raycast(transform.position, Vector3.down, out hitInfo, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        if(hit) {
            Debug.Log(hitInfo.point);
            Debug.DrawLine(transform.position, hitInfo.point, Color.green);
            Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal * 0.2f, Color.blue);
        }
    }

    void OnValuesUpdated() {
		if (!Application.isPlaying) {
            UnityEditor.EditorApplication.update -= OnValuesUpdated;
			cast();
		}
	}

    // Ensure values can't be edited outside of the valid range
    void OnValidate() {
        UnityEditor.EditorApplication.update += OnValuesUpdated;
    }
}

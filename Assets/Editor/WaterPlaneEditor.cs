using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(WaterPlane))]
public class WaterPlaneEditor : Editor {

    public override void OnInspectorGUI() {
        WaterPlane waterPlane = target as WaterPlane;

        // DrawDefaultInspector returns true if any value in the inspector gets changed
        if(DrawDefaultInspector()) {
            waterPlane.GenerateMesh();
        }

        if(GUILayout.Button("GenerateMesh")) {
            waterPlane.GenerateMesh();
            EditorUtility.SetDirty(target);
            // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
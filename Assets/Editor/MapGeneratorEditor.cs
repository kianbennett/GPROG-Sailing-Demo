using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public override void OnInspectorGUI() {
        MapGenerator mapGenerator = target as MapGenerator;

        // DrawDefaultInspector returns true if any value in the inspector gets changed
        if(DrawDefaultInspector()) {
            if(mapGenerator.autoUpdate) {
                mapGenerator.GenerateMap();
            }
        }

        if(GUILayout.Button("Generate")) {
            mapGenerator.GenerateMap();
            EditorUtility.SetDirty(target);
            // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        if(GUILayout.Button("Spawn Objects")) {
            mapGenerator.GenerateObjects();
            EditorUtility.SetDirty(target);
            // EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        if(GUILayout.Button("Next Islands")) {
            mapGenerator.NextIslands();
            EditorUtility.SetDirty(target);
        }
    }
}
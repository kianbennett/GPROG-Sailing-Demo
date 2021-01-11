using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(Ship))]
public class ShipEditor : Editor {

    public override void OnInspectorGUI() {
        Ship ship = target as Ship;

        // DrawDefaultInspector returns true if any value in the inspector gets changed
        if(DrawDefaultInspector()) {
        }

        if(GUILayout.Button("Recaculate Ropes")) {
            ship.CalculateRopePoints(ship.mastRopePointF.position, ship.mastRopePointB.position, ship.mastRopeRenderer, ship.mastRopeSlack);
            ship.CalculateRopePoints(ship.hullRopePointL.position, ship.sailRopePointL.position, ship.sailRopeRendererL, ship.sailRopeSlackL);
            ship.CalculateRopePoints(ship.hullRopePointR.position, ship.sailRopePointR.position, ship.sailRopeRendererR, ship.sailRopeSlackR);
            EditorUtility.SetDirty(target);
        }
    }
}
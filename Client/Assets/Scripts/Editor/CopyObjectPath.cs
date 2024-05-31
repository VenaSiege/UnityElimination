using UnityEditor;
using UnityEngine;

namespace Editor {

public class CopyObjectPath {

    [MenuItem("GameObject/Show World Transform", false, 2001)]
    private static void ShowWorldTransform() {
        Transform ts = Selection.activeTransform;
        if (ts) {
            EditorUtility.DisplayDialog("World Transform",
                $"Position: {ts.position}\nRotation: {ts.eulerAngles}\nScale: {ts.lossyScale}",
                "OK");
        } else {
            EditorUtility.DisplayDialog("World Transform", "No GameObject Selected!", "OK");
        }
    }

    [MenuItem("GameObject/Copy Full Path", false, 2000)]
    private static void Execute() {
        var ts = Selection.activeTransform;
        if (!ts) {
            EditorUtility.DisplayDialog("Warning", "No GameObject has been selected.", "OK");
            return;
        }

        string path = ts.name;
        for (;;) {
            ts = ts.parent;
            if (!ts) {
                break;
            }
            path = $"{ts.name}/{path}";
        }

        EditorGUIUtility.systemCopyBuffer = path;
    }

    [MenuItem("GameObject/Copy Full Path", true)]
    private static bool Validation() {
        return Selection.activeTransform;
    }
}

}
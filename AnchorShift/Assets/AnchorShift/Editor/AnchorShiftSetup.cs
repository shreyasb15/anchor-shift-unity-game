using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnchorShift.Editor
{
    public static class AnchorShiftSetup
    {
        [MenuItem("Anchor Shift/Build Playable Scene")]
        public static void CreateScene()
        {
            const string path = "Assets/Scenes/AnchorShift.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                EditorSceneManager.OpenScene(path);
                Debug.Log("Anchor Shift scene already exists: " + path);
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(17, 25, 27, 255);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            new GameObject("Anchor Shift").AddComponent<AnchorShiftGame>();
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Anchor Shift scene created: " + path);
        }
    }
}

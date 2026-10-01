using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>Tools/Casual Game/Theme Lab: (re)creates the shader / theme lab scene and opens it (Docs/SHADER_LAB.md).</summary>
    public static class ThemeLabBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Lab/ThemeLab.unity";

        [MenuItem("Tools/Casual Game/Theme Lab")]
        public static void Open()
        {
            if (!System.IO.File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public static void Build()
        {
            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes/Lab");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0, 0, -10);
            // by name: the lab assembly is not a reference of the editor tools (keeps it out of their build checks)
            new GameObject("ThemeLab").AddComponent(System.Type.GetType("CasualGame.Lab.ThemeLab, CasualGame.Lab"));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}

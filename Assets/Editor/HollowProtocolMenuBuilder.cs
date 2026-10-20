using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menus del editor para la entrega: configurar Build Settings con el menu principal y el nivel,
/// y compilar el juego para Windows en un .zip.
/// </summary>
public static class HollowProtocolMenuBuilder
{
    const string LevelPath = "Assets/Scenes/SampleScene.unity";
    const string MenuPath = "Assets/Scenes/Main Menu.unity";

    /// <summary>
    /// Deja "Main Menu" (0) y "SampleScene" (1) en Build Settings. Si la escena del menu ya existe solo
    /// le agrega el componente MainMenu cuando falta; si no existe, la crea.
    /// </summary>
    [MenuItem("Tools/Hollow Protocol/Configurar menu principal y Build Settings")]
    public static void SetupMainMenu()
    {
        if (!File.Exists(LevelPath)) { EditorUtility.DisplayDialog("Hollow Protocol", "No encontré " + LevelPath, "OK"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        bool existed = File.Exists(MenuPath);
        var scene = existed
            ? EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu == null)
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
                camGo.AddComponent<AudioListener>();
            }

            var go = new GameObject("MainMenu");
            menu = go.AddComponent<MainMenu>();
            menu.levelScene = "SampleScene";

            var music = FindClip("dark-horror-soundscape");
            if (music == null) music = FindClip("creepy-atmosphere");
            if (music != null)
            {
                var so = new SerializedObject(menu);
                so.FindProperty("menuMusic").objectReferenceValue = music;
                so.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.SaveScene(scene, MenuPath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuPath, true),
            new EditorBuildSettingsScene(LevelPath, true)
        };

        EditorSceneManager.OpenScene(LevelPath);   // vuelve al nivel
        Debug.Log("[Hollow Protocol] Build Settings: 0 = Main Menu, 1 = SampleScene.");
        EditorUtility.DisplayDialog("Hollow Protocol",
            "Listo.\n\nBuild Settings:\n  0  Main Menu\n  1  SampleScene\n\nPara probar el menú abre Assets/Scenes/Main Menu.unity y pulsa Play.", "OK");
    }

    /// <summary>Compila para Windows 64 bits en Build_Videojuego/ y lo comprime en Build_Videojuego.zip.</summary>
    [MenuItem("Tools/Hollow Protocol/Compilar juego Windows (.zip)")]
    public static void BuildWindows()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Hollow Protocol", "Build Settings no tiene escenas. Corre primero 'Configurar menu principal y Build Settings'.", "OK");
            return;
        }

        string root = Path.GetDirectoryName(Application.dataPath);
        string dir = Path.Combine(root, "Build_Videojuego");
        string zip = Path.Combine(root, "Build_Videojuego.zip");
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(dir, "TheHollowProtocol.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Hollow Protocol", "La compilación falló: " + report.summary.result + ". Revisa la consola.", "OK");
            return;
        }

        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(dir, zip);

        long mb = new FileInfo(zip).Length / (1024 * 1024);
        Debug.Log("[Hollow Protocol] Build listo: " + zip + " (" + mb + " MB)");
        EditorUtility.DisplayDialog("Hollow Protocol", "Build listo.\n\n" + zip + "\n(" + mb + " MB)\n\nSúbelo a Google Drive/Mega y entrega el link.", "OK");
        EditorUtility.RevealInFinder(zip);
    }

    static AudioClip FindClip(string namePart)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(namePart))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}

using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura las animaciones de Mixamo del enemigo: pasa los FBX de Assets/_Animation a Humanoid con bucle,
/// crea el Animator Controller (reposo / gatear segun "Speed") y se lo pone al enemigo de la escena.
/// </summary>
public static class HollowProtocolEnemyAnimation
{
    const string AnimFolder = "Assets/_Animation";
    const string ModelPath = "Assets/_Models/shia lebeuofsf.fbx";
    const string ControllerPath = "Assets/_Animation/Enemigo.controller";
    const string IdleFile = "Kneeling Idle";
    const string CrawlFile = "Running Crawl";

    [MenuItem("Tools/Hollow Protocol/Configurar animaciones del enemigo (Mixamo)")]
    public static void Setup()
    {
        var log = new System.Text.StringBuilder("[Hollow Protocol] Animaciones del enemigo:\n");

        var idle = PrepareClip(AnimFolder + "/" + IdleFile + ".fbx", log);
        var crawl = PrepareClip(AnimFolder + "/" + CrawlFile + ".fbx", log);
        if (idle == null || crawl == null) { Debug.LogError(log.ToString()); return; }

        // ---- Animator Controller: Reposo <-> Gatear segun Speed
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Reposo"); sIdle.motion = idle;
        var sCrawl = sm.AddState("Gatear"); sCrawl.motion = crawl;
        sm.defaultState = sIdle;
        var t1 = sIdle.AddTransition(sCrawl); t1.hasExitTime = false; t1.duration = 0.2f;
        t1.AddCondition(AnimatorConditionMode.Greater, 0.35f, "Speed");
        var t2 = sCrawl.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.3f;
        t2.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
        log.AppendLine("OK     Controller " + ControllerPath + " (Reposo / Gatear)");

        // ---- Avatar del monstruo
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isHuman)
            log.AppendLine("FALTA  el modelo " + ModelPath + " no tiene Avatar Humanoid valido (Rig > Animation Type: Humanoid > Apply).");
        else log.AppendLine("OK     Avatar humanoid del monstruo");

        // ---- Enemigo de la escena
        var enemy = Object.FindFirstObjectByType<Enemy>(FindObjectsInactive.Include);
        if (enemy == null) { log.AppendLine("FALTA  no hay Enemy en la escena abierta"); Debug.LogWarning(log.ToString()); return; }

        Transform armature = enemy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Armature");
        GameObject host = armature != null && armature.parent != null ? armature.parent.gameObject : enemy.gameObject;
        var anim = host.GetComponent<Animator>();
        if (anim == null) anim = Undo.AddComponent<Animator>(host);
        Undo.RecordObject(anim, "Animaciones del enemigo");
        anim.runtimeAnimatorController = ctrl;
        if (avatar != null) anim.avatar = avatar;
        anim.applyRootMotion = false;                     // al enemigo lo mueve su script
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.enabled = true;
        EditorUtility.SetDirty(anim);
        log.AppendLine("OK     Animator en " + host.name + " (" + enemy.name + ")");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        log.AppendLine("Guarda la escena (Ctrl+S). La animacion por codigo (QuadrupedCrawl) se desactiva sola mientras haya Animator.");
        Debug.Log(log.ToString());
    }

    // Pasa el FBX a Humanoid, con bucle y sin mover la raiz, y devuelve su clip.
    static AnimationClip PrepareClip(string path, System.Text.StringBuilder log)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) { log.AppendLine("FALTA  " + path); return null; }

        if (imp.animationType != ModelImporterAnimationType.Human || imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }

        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = true;
            c.loopPose = true;
            c.lockRootRotation = true;        // Bake Into Pose: no gira al personaje
            c.keepOriginalOrientation = true;
            c.lockRootHeightY = true;         // no lo sube ni lo baja
            c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true;      // en el sitio
            c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();

        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        log.AppendLine(clip != null ? "OK     " + System.IO.Path.GetFileName(path) + " -> Humanoid, en bucle (" + clip.name + ")"
                                    : "FALTA  " + path + " no tiene clip de animacion");
        return clip;
    }
}

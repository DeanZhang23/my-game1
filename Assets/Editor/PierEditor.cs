using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NiaIsland.EditorTools
{
    public static class PierEditor
    {
        const string ScenePath = "Assets/Scenes/Pier.unity";

        [MenuItem("Nia Island/Open Pier Scene")]
        public static void OpenPierScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    [InitializeOnLoad]
    static class LegacyInputEnforcer
    {
        static LegacyInputEnforcer()
        {
            EditorApplication.delayCall += EnsureLegacyInput;
        }

        static void EnsureLegacyInput()
        {
            if (SessionState.GetBool("NiaIsland.LegacyInputChecked", false))
                return;

            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0)
                    return;

                SessionState.SetBool("NiaIsland.LegacyInputChecked", true);
                for (var i = 0; i < assets.Length; i++)
                {
                    var asset = assets[i];
                    if (asset == null)
                        continue;

                    var serialized = new SerializedObject(asset);
                    var handler = serialized.FindProperty("activeInputHandler");
                    // 0 = Input Manager (Old), 1 = Input System package, 2 = Both.
                    // Old or Both can read Input.GetKey and uGUI clicks. Input System only cannot.
                    if (handler == null || handler.intValue != 1)
                        continue;

                    handler.intValue = 0;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("Nia Island: Active Input Handling was set to Input Manager (Old) so pier movement and the Cast/Hook button work. If Unity asks you to restart, restart the editor, then press Play.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Nia Island could not verify Active Input Handling. Set it to Input Manager (Old) or Both before Play. " + ex.Message);
            }
        }
    }
}

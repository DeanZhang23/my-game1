using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoes.Editor
{
    [InitializeOnLoad]
    public static class EnsureUrp2DPipeline
    {
        private const string Folder = "Assets/Settings";
        private const string RendererPath = Folder + "/EchoesRenderer2D.asset";
        private const string PipelinePath = Folder + "/EchoesURP2D.asset";

        static EnsureUrp2DPipeline()
        {
            EditorApplication.delayCall += Ensure;
        }

        private static void Ensure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Ensure;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline != null && GraphicsSettings.defaultRenderPipeline == pipeline)
                return;

            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                var serialized = new SerializedObject(pipeline);
                var rendererType = serialized.FindProperty("m_RendererType");
                if (rendererType != null)
                {
                    for (int i = 0; i < rendererType.enumNames.Length; i++)
                    {
                        if (rendererType.enumNames[i] == "_2DRenderer")
                        {
                            rendererType.enumValueIndex = i;
                            break;
                        }
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }
    }
}

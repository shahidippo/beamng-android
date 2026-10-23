using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ImpactDrive.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/ImpactDrive/Scenes/ProvingGround.unity";
        private const string Generated = "Assets/ImpactDrive/Generated";

        [MenuItem("ImpactDrive/Configure project and URP")]
        public static void Configure()
        {
            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            string rendererPath = Generated + "/MobileRenderer.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            string pipelinePath = Generated + "/MobileURP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.supportsHDR = false;
                pipeline.msaaSampleCount = 2;
                pipeline.renderScale = 1f;
                pipeline.shadowDistance = 80f;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Explicit Resources materials retain runtime-created shader references in IL2CPP players.
            Directory.CreateDirectory(Generated + "/Resources");
            AssetDatabase.Refresh();
            foreach (string shaderName in new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Skybox/Procedural" })
            {
                string materialPath = Generated + "/Resources/" + shaderName.Substring(shaderName.LastIndexOf('/') + 1) + "Reference.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null) continue;
                Shader shader = Shader.Find(shaderName);
                if (shader == null) throw new BuildFailedException("Required URP shader was not imported: " + shaderName);
                AssetDatabase.CreateAsset(new Material(shader), materialPath);
            }
            string glassPath = Generated + "/Resources/GlassReference.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(glassPath) == null)
            {
                var glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                glass.SetFloat("_Surface", 1f);
                glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                glass.SetFloat("_ZWrite", 0f);
                glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                glass.SetOverrideTag("RenderType", "Transparent");
                glass.renderQueue = (int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(glass, glassPath);
            }
            // Eliminate a per-quality pipeline override so changing quality never selects built-in RP.
            int priorQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(priorQuality, true);
            PlayerSettings.companyName = "ImpactDrive";
            PlayerSettings.productName = "ImpactDrive Mobile — Engineering Build";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.impactdrive.mobile");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            // ModelImporter defaults can otherwise make damage mesh access fail in a player.
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/ImpactDrive/Art/") || !path.EndsWith(".obj")) continue;
                if (AssetImporter.GetAtPath(path) is ModelImporter importer && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.globalScale = 1f;
                    importer.SaveAndReimport();
                }
            }
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log("ImpactDrive configured: URP17, Android ARM64 IL2CPP, API26–35, GLES3. Device performance remains unverified.");
        }
    }

    public sealed class OriginalAssetImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/ImpactDrive/Art/") || !assetPath.EndsWith(".obj")) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.addCollider = false;
        }
    }
}

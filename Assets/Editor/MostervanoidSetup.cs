using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Mostervanoid.EditorTools
{
    /// <summary>
    /// Configuração do projeto: URP (asset + renderer), materiais base em Resources (garantem os shaders no build),
    /// cena principal, exportação das skins padrão para StreamingAssets/Skins e ajustes de Player Settings.
    /// Menu: Mostervanoid > Configurar Projeto.
    /// </summary>
    public static class MostervanoidSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string UrpPath = "Assets/Settings/URP_Mostervanoid.asset";
        const string RendererPath = "Assets/Settings/URP_Mostervanoid_Renderer.asset";

        [MenuItem("Mostervanoid/Configurar Projeto")]
        public static void Setup()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Materials");
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Settings");

            SetupUrp();

            // materiais base com URP/Lit (referenciados em Resources => shader e variantes entram no build)
            var lit = Shader.Find(Mat.LitShader);
            var baseMat = LoadOrCreate("Assets/Resources/Materials/BaseMat.mat", lit);
            baseMat.shader = lit;
            var glow = LoadOrCreate("Assets/Resources/Materials/GlowMat.mat", lit);
            glow.shader = lit;
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", Color.white);
            var trail = LoadOrCreate("Assets/Resources/Materials/TrailMat.mat", Shader.Find("Sprites/Default"));
            trail.shader = Shader.Find("Sprites/Default");
            foreach (var m in new[] { baseMat, glow, trail }) EditorUtility.SetDirty(m);

            ExportDefaultSkins(false);

            // cena principal (fog ligada na cena para o build manter as variantes de névoa)
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (Object.FindAnyObjectByType<GameManager>() == null)
                new GameObject("Mostervanoid").AddComponent<GameManager>();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.productName = "Mostervanoid";
            PlayerSettings.bundleVersion = "0.1-alpha";
            PlayerSettings.companyName = "Mostervanoid";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mostervanoid] Projeto configurado. Abra Assets/Scenes/Main.unity e aperte Play.");
        }

        [MenuItem("Mostervanoid/Build Windows")]
        public static void BuildWindows()
        {
            Setup();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/Mostervanoid.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Mostervanoid] Build: " + report.summary.result + " -> " + opts.locationPathName);
        }

        [MenuItem("Mostervanoid/Exportar Skins Padrão (sobrescreve)")]
        public static void ExportSkinsOverwrite() => ExportDefaultSkins(true);

        static void ExportDefaultSkins(bool overwrite)
        {
            var dir = Path.Combine(Application.dataPath, "StreamingAssets", "Skins");
            Directory.CreateDirectory(dir);
            for (int i = 0; i < Forms.Count; i++)
            {
                var path = Path.Combine(dir, $"form{i}.png");
                if (File.Exists(path) && !overwrite) continue;
                var tex = SkinLibrary.Generate(i);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            AssetDatabase.Refresh();
            Debug.Log("[Mostervanoid] Skins em " + dir);
        }

        static void SetupUrp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
            if (asset == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(asset, UrpPath);
            }
            asset.supportsHDR = true;               // bloom nos projéteis de energia
            asset.msaaSampleCount = 4;
            asset.shadowDistance = 45f;
            asset.shadowCascadeCount = 2;
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

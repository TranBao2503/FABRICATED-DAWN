using System;
using System.IO;
using FabricatedDawn.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn.CaseFile.Editor
{
    public static class CaseFileSetup
    {
        public const string ScenePath = "Assets/FabricatedDawn/Scenes/ToolkitCaseFile.unity";
        public const string IntegrationRoot = "Assets/FabricatedDawn/Integrations/HorrorCaseFile";
        private const string KitRoot = "Assets/HorrorCaseFile";

        [MenuItem("Tools/Fabricated Dawn/Dùng Horror Case File UI")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before configuring the UI scene.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(ToolkitBootstrap.ScenePath, ScenePath))
                throw new IOException("Could not create the Case File scene from the toolkit scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var original = UnityEngine.Object.FindAnyObjectByType<AdventureUI>();
            var adapter = UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if (adapter == null)
            {
                if (original == null) throw new Exception("Missing original toolkit controller.");
                adapter = original.gameObject.AddComponent<CaseFileAdventureUI>();
            }
            if (original != null) original.enabled = false;
            adapter.content = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ToolkitBootstrap.ContentPath);
            adapter.mainMenu = Screen("S01_MainMenu"); adapter.hud = Screen("S07_Hud");
            adapter.inventory = Screen("S11_Inventory"); adapter.journal = Screen("S13_Journal"); adapter.pause = Screen("S10_PauseMenu");
            adapter.overrides = AssetDatabase.LoadAssetAtPath<StyleSheet>(IntegrationRoot + "/CaseFileOverrides.uss");
            adapter.keyIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(KitRoot + "/Art/Icons/BrassKey.png");
            EditorUtility.SetDirty(adapter);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(ToolkitBootstrap.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("File/Save Project");
            Verify();
            Selection.activeGameObject = adapter.gameObject;
            Debug.Log("CASE_FILE_UI_READY: " + ScenePath);
        }

        [MenuItem("Tools/Fabricated Dawn/Kiểm tra tích hợp Case File UI")]
        public static void Verify()
        {
            var adapter = UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if (adapter == null) throw new Exception("Open ToolkitCaseFile scene before validation.");
            var report = ToolkitVerification.Run(adapter.content);
            var legacy = Verification.Run(); report.passed.AddRange(legacy.passed); report.failed.AddRange(legacy.failed);
            Check(report, "Main menu template exposes navigation", () => Template(adapter.mainMenu, "menu-item-01", "menu-item-02", "title"));
            Check(report, "HUD template exposes objective and gameplay host", () => Template(adapter.hud, "gameplay-view", "objective-title", "gear"));
            Check(report, "Inventory template exposes grid and item actions", () => Template(adapter.inventory, "grid", "detail-title", "btn-equip", "btn-examine"));
            Check(report, "Journal template exposes documents and reading panel", () => Template(adapter.journal, "doc-list", "page-title-1", "page-scroll"));
            Check(report, "Pause template exposes save and resume controls", () => Template(adapter.pause, "menu-item-01", "menu-item-02", "menu-item-05"));
            Check(report, "Scene references adapter, theme, item icon and data", () => {
                if (adapter.content == null || adapter.overrides == null || adapter.keyIcon == null ||
                    adapter.GetComponent<UIDocument>().panelSettings == null) throw new Exception("Missing scene dependency.");
                var old = adapter.GetComponent<AdventureUI>();
                if (old != null && old.enabled) throw new Exception("Both UI controllers are enabled.");
            });
            Check(report, "Case File scene is first in the build scene list", () => {
                var scenes = EditorBuildSettings.scenes;
                if (scenes.Length == 0 || !scenes[0].enabled || scenes[0].path != ScenePath)
                    throw new Exception("Wrong startup scene.");
            });
            const string reportPath = "Assets/FabricatedDawn/Reports/casefile-ui-verification.json";
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true)); AssetDatabase.Refresh();
            if (report.failed.Count > 0) throw new Exception(string.Join("\n", report.failed));
            Debug.Log("CASE_FILE_VERIFIED: " + report.passed.Count + " checks passed.");
        }

        private static VisualTreeAsset Screen(string name) => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(KitRoot + "/UI/Screens/" + name + ".uxml");
        private static void Template(VisualTreeAsset template, params string[] names)
        {
            if (template == null) throw new Exception("Missing template asset.");
            var root = template.CloneTree();
            foreach (var name in names) if (root.Q<VisualElement>(name) == null) throw new Exception("Missing control: " + name);
        }
        private static void Check(VerificationReport report, string name, Action test)
        { try { test(); report.passed.Add(name); } catch (Exception ex) { report.failed.Add(name + ": " + ex.Message); } }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FabricatedDawn.Editor
{
    public sealed class ToolkitWindow : EditorWindow
    {
        private ProjectCatalog catalog;
        private ContentReport report;
        private Vector2 scroll;

        [MenuItem("Tools/Fabricated Dawn/Toolkit")]
        public static void Open() => GetWindow<ToolkitWindow>("Fabricated Dawn");

        private void OnEnable() => catalog = AssetDatabase.LoadAssetAtPath<ProjectCatalog>(ProjectScaffolder.CatalogPath);

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("Fabricated Dawn · Toolkit 0.1.0", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ chuẩn bị dự án 2D point-and-click. Tài liệu Google Docs chỉ được đọc; gói này không chứa chức năng ghi tài liệu.", MessageType.Info);
            if (GUILayout.Button("Tạo cấu trúc và dữ liệu 8 màn (giữ asset đã có)")) catalog = ProjectScaffolder.Create();
            catalog = (ProjectCatalog)EditorGUILayout.ObjectField("Catalog", catalog, typeof(ProjectCatalog), false);
            if (GUILayout.Button("Kiểm tra dữ liệu")) report = ContentValidator.Inspect(catalog);
            if (GUILayout.Button("Tạo / mở prototype M4"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    ProjectScaffolder.Create();
                    EditorSceneManager.OpenScene(ProjectScaffolder.CreateDemoScene());
                }
            }
            if (GUILayout.Button("Xuất báo cáo JSON cho Codex"))
            {
                report = ContentValidator.Inspect(catalog);
                Directory.CreateDirectory(ProjectScaffolder.Root + "/Reports");
                File.WriteAllText(ProjectScaffolder.Root + "/Reports/content-report.json", JsonUtility.ToJson(report, true));
                AssetDatabase.Refresh();
            }
            if (GUILayout.Button("Chạy kiểm tra trạng thái và checkpoint"))
            {
                var tests = Verification.Run();
                Debug.Log(JsonUtility.ToJson(tests, true));
                EditorUtility.DisplayDialog("Verification", $"Passed: {tests.passed.Count}\nFailed: {tests.failed.Count}", "OK");
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quy tắc từ tài liệu", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("M4: đổi toàn bộ không gian sau thao tác mở cửa.\nM6: phá bảo hộ phòng thờ, độc lập với hình nền.\nDao: chỉ làm khựng hai vong vợ/chồng trong không gian oán niệm.\nGame Over tải checkpoint; Bad Ending là nhánh cốt truyện.", MessageType.None);
            if (report != null)
            {
                GUILayout.Label($"{report.chapterCount} màn · {report.roomCount} phòng · {report.itemCount} vật phẩm");
                foreach (var error in report.errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                foreach (var warning in report.warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}

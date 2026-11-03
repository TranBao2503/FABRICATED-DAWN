using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn.Editor
{
    public static class ToolkitBootstrap
    {
        public const string ContentPath = "Assets/FabricatedDawn/Data/Sandbox/ToolkitAdventure.asset";
        public const string ScenePath = "Assets/FabricatedDawn/Scenes/ToolkitSandbox.unity";
        public const string PackageRoot = "Packages/com.4koq.fabricated-dawn";

        public static void SetupProject()
        {
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "4KoQ";
            PlayerSettings.productName = "Fabricated Dawn";
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            ProjectScaffolder.Create();
            var content = CreateContent();
            CreateScene(content);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorSceneManager.OpenScene(ScenePath);
            RepairGeneratedBindings();
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("FABRICATED_DAWN_PROJECT_READY: " + Path.GetFullPath(ScenePath));
        }

        public static AdventureDefinition CreateContent()
        {
            if (File.Exists(ContentPath))
            {
                var existing = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
                if (existing == null) throw new IOException("Adventure path is occupied.");
                if (existing.contentId == "fabricated-dawn-toolkit-sandbox-v1" && existing.objectiveUpdates.Length == 0)
                {
                    existing.objectiveUpdates = DefaultObjectives();
                    EditorUtility.SetDirty(existing); AssetDatabase.SaveAssets();
                }
                return existing;
            }
            Directory.CreateDirectory("Assets/FabricatedDawn/Data/Sandbox"); AssetDatabase.Refresh();
            var key = Item("ITEM_SANDBOX_KEY", "Chìa khóa tủ", "Mở tủ gỗ trong cảnh thử.");
            var stone = Item("ITEM_SANDBOX_STONE", "Cục chặn giấy", "Vật phẩm thử phản hồi khi dùng sai mục tiêu.");
            var c = ScriptableObject.CreateInstance<AdventureDefinition>();
            c.contentId = "fabricated-dawn-toolkit-sandbox-v1";
            c.startRoomId = "ROOM_01";
            c.initialObjective = "Tìm cách mở tủ gỗ. Manh mối bên trong sẽ giúp mở lối ra hành lang.";
            c.completionPuzzleId = "PUZZLE_SANDBOX_DONE";
            c.objectiveUpdates = DefaultObjectives();
            c.items = new[] { key, stone };
            c.clues = new[] {
                new ClueDefinition { id = "CLUE_SANDBOX_NOTE", title = "Tờ giấy trong tủ",
                    body = "NỘI DUNG THỬ HỆ THỐNG\n\nChìa khóa đã mở tủ. Việc đọc tờ giấy này được ghi vào sổ và mở quyền đi ra hành lang.\n\nTrong game chính thức, điều kiện này sẽ được thay bằng chuỗi manh mối do nhóm thiết kế duyệt." },
                new ClueDefinition { id = "CLUE_SANDBOX_SIGN", title = "Bảng tin hành lang",
                    body = "Bạn đã hoàn thành vòng chơi mẫu: nhặt → chọn → dùng → đọc → mở lối → chuyển cảnh.\n\nHãy quay lại căn phòng để kiểm tra: đồ đã nhặt không xuất hiện lại, tủ vẫn mở và các manh mối vẫn còn trong sổ." }
            };
            c.dialogues = new[] {
                new DialogueDefinition { id = "DIALOGUE_SANDBOX_FRIEND", speaker = "Bạn cùng phòng · thoại thử",
                    lines = new[] { "Thử kiểm tra mặt bàn xem có chìa khóa không.",
                        "Nhặt được rồi thì chọn chìa trong túi đồ, sau đó nhấn vào chiếc tủ.",
                        "Mình chỉ là nhân vật thử hội thoại. Tên và lời thoại chính thức sẽ do nhóm chốt sau." } }
            };
            c.rooms = new[] {
                new AdventureRoom { id = "ROOM_01", title = "Phòng trọ", subtitle = "01 / PHÒNG THỬ TƯƠNG TÁC",
                    description = "Khung cảnh cấu hình bằng asset, thay hình nền sau mà giữ ID tương tác.",
                    placeholderColor = new Color(.25f, .30f, .25f), hotspots = new[] {
                        new AdventureHotspot { id = "HP_FRIEND", label = "Bạn cùng phòng\nNói chuyện", kind = InteractionKind.Talk,
                            bounds = new Rect(.055f,.30f,.17f,.43f), visualClass = "friend", dialogueId = "DIALOGUE_SANDBOX_FRIEND", response = "Bạn cùng phòng đang nói chuyện." },
                        new AdventureHotspot { id = "HP_KEY", label = "Chìa khóa\nNhặt", kind = InteractionKind.Pickup,
                            bounds = new Rect(.29f,.60f,.15f,.14f), visualClass = "key", item = key, response = "Đã nhặt chìa khóa tủ. Chọn chìa trong túi rồi nhấn vào tủ." },
                        new AdventureHotspot { id = "HP_STONE", label = "Chặn giấy\nNhặt", kind = InteractionKind.Pickup,
                            bounds = new Rect(.30f,.38f,.13f,.12f), visualClass = "stone", item = stone, response = "Đã nhặt cục chặn giấy." },
                        new AdventureHotspot { id = "HP_CABINET", label = "Tủ gỗ\nĐang khóa", kind = InteractionKind.UseItem,
                            bounds = new Rect(.59f,.23f,.21f,.51f), visualClass = "cabinet", item = key, consumeItem = true,
                            hiddenAfterPuzzle = "PUZZLE_CABINET_OPEN", solvesPuzzle = "PUZZLE_CABINET_OPEN",
                            response = "Tủ đã mở. Có một tờ giấy bên trong.", checkpointAfter = true },
                        new AdventureHotspot { id = "HP_NOTE", label = "Tủ đã mở\nĐọc tờ giấy", kind = InteractionKind.ReadClue,
                            bounds = new Rect(.59f,.23f,.21f,.51f), visualClass = "note", clueId = "CLUE_SANDBOX_NOTE",
                            visibleAfterPuzzle = "PUZZLE_CABINET_OPEN", solvesPuzzle = "PUZZLE_NOTE_READ", unlocksRoom = "HALL_01",
                            response = "Đã ghi tờ giấy vào sổ. Lối ra hành lang đã mở.", checkpointAfter = true },
                        new AdventureHotspot { id = "HP_EXIT", label = "Cửa phòng\nRa hành lang", kind = InteractionKind.Travel,
                            bounds = new Rect(.85f,.18f,.12f,.61f), visualClass = "door", destinationRoom = "HALL_01",
                            requiredPuzzle = "PUZZLE_NOTE_READ", response = "Bạn đã ra hành lang.", lockedResponse = "Cần kiểm tra manh mối trong tủ trước khi rời phòng." }
                    } },
                new AdventureRoom { id = "HALL_01", title = "Hành lang", subtitle = "02 / THỬ CHUYỂN CẢNH VÀ GIỮ TIẾN ĐỘ",
                    placeholderColor = new Color(.19f,.26f,.25f), hotspots = new[] {
                        new AdventureHotspot { id = "HP_RETURN", label = "Cửa phòng\nQuay lại", kind = InteractionKind.Travel,
                            bounds = new Rect(.07f,.18f,.18f,.61f), visualClass = "door", destinationRoom = "ROOM_01", response = "Đã quay lại phòng. Tiến độ tương tác được giữ nguyên." },
                        new AdventureHotspot { id = "HP_SIGN", label = "Bảng tin\nKiểm tra", kind = InteractionKind.ReadClue,
                            bounds = new Rect(.49f,.25f,.29f,.32f), visualClass = "sign", clueId = "CLUE_SANDBOX_SIGN",
                            solvesPuzzle = "PUZZLE_SANDBOX_DONE", response = "Hoàn thành cảnh thử toolkit. Bạn có thể quay lại hoặc tải checkpoint.", checkpointAfter = true }
                    } }
            };
            AssetDatabase.CreateAsset(c, ContentPath); AssetDatabase.SaveAssets();
            return c;
        }

        private static AdventureObjective[] DefaultObjectives() => new[] {
            new AdventureObjective { afterPuzzle = "PUZZLE_CABINET_OPEN", text = "Tủ đã mở. Đọc tờ giấy bên trong." },
            new AdventureObjective { afterPuzzle = "PUZZLE_NOTE_READ", text = "Lối hành lang đã mở. Ra ngoài và kiểm tra bảng tin." }
        };

        private static void RepairGeneratedBindings()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<AdventureUI>();
            if (controller == null) throw new Exception("Sandbox scene has no AdventureUI.");
            var document = controller.GetComponent<UIDocument>();
            if (document.panelSettings == null)
            {
                document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/FabricatedDawn/UI/ToolkitPanel.asset");
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                EditorSceneManager.SaveScene(controller.gameObject.scene);
            }
        }

        private static ItemDefinition Item(string id, string label, string description)
        {
            var path = "Assets/FabricatedDawn/Data/Sandbox/" + id + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException("Item path occupied: " + path);
            var item = ScriptableObject.CreateInstance<ItemDefinition>(); item.id = id; item.displayName = label;
            item.purpose = description; item.uses = 1;
            AssetDatabase.CreateAsset(item, path); return item;
        }

        private static void CreateScene(AdventureDefinition content)
        {
            if (File.Exists(ScenePath)) return;
            var panelPath = "Assets/FabricatedDawn/UI/ToolkitPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1440, 900);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = .5f;
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(PackageRoot + "/Runtime/UI/DefaultTheme.tss");
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
            var cam = camera.GetComponent<Camera>(); cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.05f,.08f,.06f);
            var go = new GameObject("Toolkit Adventure", typeof(UIDocument));
            var document = go.GetComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            var controller = go.AddComponent<AdventureUI>(); controller.content = content;
            controller.styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageRoot + "/Runtime/UI/Adventure.uss");
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Fabricated Dawn/Mở cảnh toolkit")]
        public static void OpenSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ProjectScaffolder.Create(); CreateScene(CreateContent()); EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/Fabricated Dawn/Chọn dữ liệu tương tác")]
        public static void SelectContent() => Selection.activeObject = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);

        public static void Verify()
        {
            var content = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            var errors = AdventureValidator.Errors(content);
            if (errors.Count > 0) throw new Exception(string.Join("\n", errors));
            var report = ToolkitVerification.Run(content);
            var sceneController = UnityEngine.Object.FindAnyObjectByType<AdventureUI>();
            if (sceneController != null && sceneController.content == content && sceneController.styleSheet != null &&
                sceneController.GetComponent<UIDocument>().panelSettings != null)
                report.passed.Add("Sandbox scene has its content, stylesheet and panel references");
            else report.failed.Add("Sandbox scene is missing a UI dependency");
            var legacy = Verification.Run();
            report.passed.AddRange(legacy.passed); report.failed.AddRange(legacy.failed);
            string reports = "Assets/FabricatedDawn/Reports";
            Directory.CreateDirectory(reports);
            File.WriteAllText(reports + "/toolkit-verification.json", JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (report.failed.Count > 0) throw new Exception(string.Join("\n", report.failed));
            Debug.Log("TOOLKIT_VERIFIED: " + report.passed.Count + " checks passed.");
        }
    }
}

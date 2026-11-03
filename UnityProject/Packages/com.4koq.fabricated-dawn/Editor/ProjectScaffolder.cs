using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FabricatedDawn.Editor
{
    public static class ProjectScaffolder
    {
        public const string Root = "Assets/FabricatedDawn";
        public const string CatalogPath = Root + "/Data/ProjectCatalog.asset";
        public const string SourceUrl = "https://docs.google.com/document/d/1AlE5j8_13of6geX94sKJDvszqVpkJn0DIIvbFeWY168/edit";

        public static ProjectCatalog Create()
        {
            foreach (var folder in new[] { "Data/Chapters", "Data/Rooms", "Data/Items", "Scenes", "Art/Present",
                "Art/Past", "Art/Items", "Audio/Ambience", "Audio/Spirits", "UI", "Dialogue", "Reports" })
                EnsureFolder(Root + "/" + folder);
            var existing = AssetDatabase.LoadAssetAtPath<ProjectCatalog>(CatalogPath);
            if (existing != null) return existing; // Never overwrite designer-authored content.
            if (File.Exists(CatalogPath)) throw new IOException("Catalog path is occupied by a different asset.");
            var catalog = ScriptableObject.CreateInstance<ProjectCatalog>();
            catalog.sourceDocumentUrl = SourceUrl;
            string[] titles = { "Những ngày ở trọ", "Người thứ năm", "Chuyện nhà chủ cũ", "Cánh cửa trở về",
                "Những người chưa đi", "Lời báo mộng", "Sự thật bị chôn giấu", "Đêm cuối" };
            string[] goals = { "Làm quen bản đồ, bạn cùng trọ và các dấu hiệu lạ.",
                "Trải qua Vỗ Vai Ma và tìm tới phòng thờ.", "Điều tra án mạng và quyết định chuyển trọ.",
                "Giải ma che mắt, mở lối phụ, bước qua cánh cửa.", "Giải đồ chơi và ngăn tủ người chồng.",
                "Ghép lời báo mộng, thoát khi phòng thờ bị xâm nhập.",
                "Đối chiếu ba cái chết, tìm dao ký ức, học tác dụng dao.", "Đối diện người mẹ bằng dao, manh mối và lựa chọn." };
            string[] criteria = { "Hoàn tất sinh hoạt bắt buộc; chuyển sang buổi tối chơi Vỗ Vai Ma.",
                "Chạm vai người thứ năm; sống sót tới phòng thờ.", "Đủ thông tin án mạng và thống nhất rời nhà.",
                "M4_LOOP + M4_SIDE_DOOR hoàn tất; sự kiện M4_DOOR_OPENED.",
                "Đồ chơi và ngăn tủ hoàn tất; đủ manh mối chính.", "M6_DREAM hoàn tất; SANCTUARY_BROKEN; thoát lần tấn công đầu.",
                "M7_THREE_DEATHS; dao đã nhặt; M7_KNIFE_LEARNED.", "M8_CONFRONTATION_DONE; chọn ending đủ điều kiện." };
            string[][] checkpoints = { new[] { "CP_M1_BEFORE_RITUAL" }, new[] { "CP_M2_SHRINE" },
                new[] { "CP_M3_MOVE_OUT" }, new[] { "CP_M4_BEFORE_DOOR", "CP_M4_AFTER_DOOR" },
                new[] { "CP_M5_TOY", "CP_M5_DRAWER", "CP_M5_BEFORE_CHASE" },
                new[] { "CP_M6_BEFORE_INTRUSION", "CP_M6_AFTER_ESCAPE" },
                new[] { "CP_M7_DEATHS", "CP_M7_KNIFE" }, new[] { "CP_M8_BEFORE_BOSS", "CP_M8_BEFORE_CHOICE" } };
            catalog.chapters = new ChapterDefinition[8];
            for (int i = 0; i < 8; i++)
            {
                int n = i;
                catalog.chapters[i] = CreateAsset<ChapterDefinition>(Root + $"/Data/Chapters/M{i + 1:00}.asset", c =>
                {
                    c.id = $"CHAPTER_{n + 1:00}"; c.number = n + 1; c.displayName = titles[n];
                    c.startingWorld = n < 4 ? WorldPhase.Present : WorldPhase.PastEnclosed;
                    c.objective = goals[n]; c.completionCriteria = criteria[n]; c.checkpoints = checkpoints[n];
                });
            }
            string[] roomIds = { "ROOM_01", "KITCHEN_01", "HALL_01", "STAIRS_01", "BATH_01", "SHRINE_01", "STUDY_01", "SIDE_DOOR_01" };
            string[] roomNames = { "Phòng sinh hoạt", "Nhà bếp", "Hành lang", "Cầu thang", "Khu vệ sinh", "Phòng thờ", "Phòng cũ", "Lối phụ" };
            catalog.rooms = new RoomDefinition[roomIds.Length];
            for (int i = 0; i < roomIds.Length; i++)
            {
                int n = i;
                catalog.rooms[i] = CreateAsset<RoomDefinition>(Root + "/Data/Rooms/" + roomIds[i] + ".asset", r =>
                { r.id = roomIds[n]; r.displayName = roomNames[n]; r.hasOutsideExit = n == 3 || n == 7; });
            }
            string[] itemIds = { "ITEM_FAMILY_CHARM", "ITEM_INCENSE", "ITEM_MATCHES", "ITEM_FLASHLIGHT", "ITEM_BATTERY",
                "ITEM_DOOR_TALISMAN", "ITEM_COLORED_THREAD", "ITEM_SMALL_BOWL", "ITEM_TOY", "ITEM_RENTAL_PAPERS",
                "ITEM_MEDICAL_NOTES", "ITEM_CASSETTE", "ITEM_MEMORY_KNIFE" };
            string[] itemNames = { "Vật hộ thân dòng họ", "Nhang", "Diêm", "Đèn pin", "Pin", "Bùa trấn cửa", "Chỉ ngũ sắc",
                "Bát sứ nhỏ (bản ký ức)", "Đồ chơi (bản ký ức)", "Giấy thuê nhà", "Sổ bệnh (ký ức)", "Băng cassette (ký ức)", "Con dao gây án (ký ức)" };
            catalog.items = new ItemDefinition[itemIds.Length];
            for (int i = 0; i < itemIds.Length; i++)
            {
                int n = i;
                catalog.items[i] = CreateAsset<ItemDefinition>(Root + "/Data/Items/" + itemIds[i] + ".asset", item =>
                {
                    item.id = itemIds[n]; item.displayName = itemNames[n];
                    item.pastOnly = n == 7 || n == 8 || n >= 10;
                    item.origin = n == 12 ? ItemOrigin.Linked : item.pastOnly ? ItemOrigin.Memory : ItemOrigin.Physical;
                    item.purpose = n == 12 ? "Làm khựng vong vợ/chồng tại điểm tương tác; không hại vong nhi, không giết boss."
                        : "Dữ liệu khởi tạo; nhóm thiết kế cần bổ sung điều kiện nhận và sử dụng.";
                });
            }
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            return catalog;
        }

        public static string CreateDemoScene()
        {
            const string path = Root + "/Scenes/M4_DeveloperPrototype.unity";
            EnsureFolder(Root + "/Scenes");
            if (File.Exists(path)) return path;
            var previous = SceneManager.GetActiveScene();
            bool useSingle = !previous.IsValid() || string.IsNullOrEmpty(previous.path);
            if (previous.IsValid() && useSingle && (previous.isDirty || previous.rootCount > 0))
                throw new System.InvalidOperationException("Save the untitled scene before creating the prototype.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                useSingle ? NewSceneMode.Single : NewSceneMode.Additive);
            var go = new GameObject("M4 Developer Prototype");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<DoorPrototype>();
            var camera = new GameObject("Main Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(camera, scene);
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.08f, .1f, .09f);
            EditorSceneManager.SaveScene(scene, path);
            if (!useSingle)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            return path;
        }

        private static T CreateAsset<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException("Asset path is occupied: " + path);
            var asset = ScriptableObject.CreateInstance<T>(); initialize(asset);
            AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

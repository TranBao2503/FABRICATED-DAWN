using System;
using System.IO;
using System.Linq;
using FabricatedDawn.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FabricatedDawn.CaseFile.Editor
{
    public static class StoryDemoSetup
    {
        private const string ContentPath = "Assets/FabricatedDawn/Data/Demo/StoryDemo.asset";
        private const string ScenePath = "Assets/FabricatedDawn/Scenes/StoryDemo.unity";
        private const string SandboxPath = "Assets/FabricatedDawn/Data/Sandbox/ToolkitAdventure.asset";

        [MenuItem("Tools/Fabricated Dawn/Demo/Chuẩn bị và mở demo 8 màn")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Dừng Play trước khi mở demo.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var content = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            if (content == null)
            {
                Directory.CreateDirectory("Assets/FabricatedDawn/Data/Demo");
                AssetDatabase.Refresh();
                content = CreateContent();
                AssetDatabase.CreateAsset(content, ContentPath);
            }
            PrologueSetup.Apply(content);
            EditorUtility.SetDirty(content);
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset(CaseFileSetup.ScenePath, ScenePath))
                throw new IOException("Không tạo được scene demo.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var ui = UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if (ui == null) throw new InvalidOperationException("Scene thiếu giao diện Case File.");
            ui.content = content;
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("STORY_DEMO_READY: " + ScenePath);
        }

        private static AdventureDefinition CreateContent()
        {
            var sandbox = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(SandboxPath);
            if (sandbox == null) throw new InvalidOperationException("Thiếu dữ liệu cảnh thử.");
            var content = UnityEngine.Object.Instantiate(sandbox);
            content.name = "StoryDemo";
            content.contentId = "fabricated-dawn-story-demo-m1-m2-v1";
            content.isStoryDemo = true;
            content.initialObjective = "Màn 1 · Tìm chìa khóa, mở tủ và đọc lời nhắn trước khi ra khỏi phòng.";
            content.completionPuzzleId = "DEMO_NORMAL_END";
            content.completedObjective = "Đã sống sót qua đêm đầu tiên.";
            content.objectiveUpdates = new[] {
                Objective("PUZZLE_CABINET_OPEN", "Đọc lời nhắn trong tủ."),
                Objective("PUZZLE_NOTE_READ", "Xuống phòng khách, kiểm tra mâm cơm rồi tìm đường tới phòng thờ."),
                Objective("DEMO_BOWL", "Trở lại hành lang tầng 2 và quan sát phòng thờ."),
                Objective("DEMO_ROUTE", "Đã thuộc đường trú ẩn. Xuống phòng khách nói chuyện với các bạn."),
                Objective("DEMO_GAME_START", "Đi lần lượt qua bốn góc phòng theo luật Vỗ Vai Ma."),
                Objective("DEMO_CORNER_4", "Có người ở góc trống! Chạy ra cầu thang và tìm tới phòng thờ.")
            };
            content.rooms[0].title = "Phòng ngủ của nhóm";
            content.rooms[0].subtitle = "MÀN 1 · NHỮNG NGÀY Ở TRỌ";
            content.rooms[0].chapter = 1;
            content.rooms[1].title = "Hành lang tầng 2";
            content.rooms[1].subtitle = "MÀN 1 · HỌC ĐƯỜNG ĐI";
            content.rooms[1].chapter = 1;
            var map = content.rooms[1].hotspots.First(h => h.id == "HP_SIGN");
            map.label = "Sơ đồ nhà\nGhi nhớ đường";
            map.solvesPuzzle = "";
            map.response = "Phòng thờ ở cuối hành lang tầng 2. Phòng khách nằm dưới cầu thang.";
            map.artwork = Art("fd-081");
            content.clues.First(c => c.id == "CLUE_SANDBOX_NOTE").body =
                "Hà Nội, năm 2004. Nam ở cùng Bia, Băng và Bảo trong căn nhà bà họ hàng cho thuê.\n\nLời nhắn của Bia: Xuống ăn cơm nhé. Phòng khách ở tầng dưới. Phòng thờ cuối hành lang tầng này, nhớ giữ yên tĩnh.";
            content.clues.First(c => c.id == "CLUE_SANDBOX_SIGN").body =
                "Phòng ngủ → hành lang tầng 2 → cầu thang → phòng khách.\n\nTừ cầu thang lên tầng 2, đi hết hành lang là phòng thờ. Ký hiệu dòng họ trên cửa phòng thờ giống vật hộ thân của Nam.";
            content.dialogues[0].speaker = "Bia";
            content.dialogues[0].lines = new[] {
                "Mấy đứa xuống phòng khách rồi. Tìm chìa mở tủ lấy lời nhắn đi.",
                "Bà dặn phòng thờ ở cuối hành lang, đừng bày đồ chắn cửa nhé." };
            content.clues = content.clues.Concat(new[] {
                Clue("DEMO_BOWL_CLUE", "Chiếc bát thứ năm", "Bốn người ăn cơm nhưng trên mâm có năm chiếc bát. Chiếc bát nhỏ có hoa văn trẻ con. Chưa ai nhận đã đặt nó lên mâm."),
                Clue("DEMO_MIRROR_CLUE", "Bóng trong gương", "Nam vừa rửa mặt thì thấy bóng một người phụ nữ phía sau. Quay lại không có ai. Dưới mép gương có một dấu bàn tay nhỏ."),
                Clue("DEMO_SHRINE_CLUE", "Ký hiệu dòng họ", "Ký hiệu trên đồ thờ giống vật hộ thân của Nam. Căn phòng yên tĩnh khác thường. Nam ghi nhớ đường từ phòng khách lên đây.")
            }).ToArray();
            content.dialogues = content.dialogues.Concat(new[] {
                new DialogueDefinition { id = "DEMO_GAME_DIALOGUE", speaker = "Bia · Băng · Bảo", lines = new[] {
                    "Bốn đứa thử Vỗ Vai Ma nhé? Mỗi người đứng một góc, tắt đèn rồi lần lượt đi vỗ vai người kế tiếp.",
                    "Đến góc đã trống thì dừng. Có bốn người thôi, làm sao còn người ở đó được...",
                    "Nếu có chuyện, đừng chạy ra ngõ. Nhớ đường lên phòng thờ tầng 2. Khi bị truy đuổi, chọn sai hướng có thể chết." } }
            }).ToArray();
            var living = Room("DEMO_LIVING", "Phòng khách", "MÀN 1 · BỐN NGƯỜI, NĂM CHIẾC BÁT", new Color(.29f,.25f,.2f),
                Read("DEMO_BOWL_HOTSPOT", "Mâm cơm\nChiếc bát nhỏ", "DEMO_BOWL_CLUE", "DEMO_BOWL", .3f, Art("fd-004")),
                Travel("DEMO_LIVING_UP", "Cầu thang\nLên tầng 2", "HALL_01", .79f),
                new AdventureHotspot { id = "DEMO_GAME_BEGIN", label = "Các bạn\nChơi Vỗ Vai Ma", kind = InteractionKind.Talk,
                    bounds = new Rect(.05f,.27f,.2f,.4f), visualClass = "friend", requiredPuzzle = "DEMO_ROUTE", hiddenAfterPuzzle = "DEMO_GAME_START",
                    dialogueId = "DEMO_GAME_DIALOGUE", solvesPuzzle = "DEMO_GAME_START", response = "Đèn tắt. Bốn người đứng vào bốn góc." },
                Corner(1, "Góc 1\nĐi tới Bia", "DEMO_GAME_START", .05f),
                Corner(2, "Góc 2\nĐi tới Băng", "DEMO_CORNER_1", .28f),
                Corner(3, "Góc 3\nĐi tới Bảo", "DEMO_CORNER_2", .51f),
                Corner(4, "Góc trống\nĐưa tay chạm vai", "DEMO_CORNER_3", .74f),
                Travel("DEMO_RUN", "Có người thứ năm!\nChạy ra cầu thang", "DEMO_CHASE_STAIRS", .4f, "DEMO_CORNER_4"));
            living.hotspots.First(h => h.id == "DEMO_BOWL_HOTSPOT").hiddenAfterPuzzle = "DEMO_GAME_START";
            living.hotspots.First(h => h.id == "DEMO_LIVING_UP").hiddenAfterPuzzle = "DEMO_GAME_START";
            living.hotspots.First(h => h.id == "DEMO_RUN").visibleAfterPuzzle = "DEMO_CORNER_4";
            var shrineInspect = Read("DEMO_SHRINE_INSPECT", "Bàn thờ\nQuan sát ký hiệu", "DEMO_SHRINE_CLUE", "DEMO_ROUTE", .4f, Art("fd-050"));
            shrineInspect.requiredPuzzle = "DEMO_BOWL";
            shrineInspect.lockedResponse = "Các bạn đang chờ ăn cơm ở tầng dưới. Hãy xuống phòng khách trước.";
            shrineInspect.checkpointAfter = true;
            var shrine = Room("DEMO_SHRINE", "Phòng thờ", "MÀN 1 · NHẬN BIẾT ĐƯỜNG TRÚ ẨN", new Color(.26f,.2f,.17f),
                shrineInspect, Travel("DEMO_SHRINE_BACK", "Cửa phòng\nRa hành lang", "HALL_01", .08f));
            var bathroom = Room("DEMO_BATHROOM", "Nhà vệ sinh", "MÀN 1 · BÓNG TRONG GƯƠNG", new Color(.18f,.25f,.25f),
                Read("DEMO_MIRROR", "Gương\nRửa mặt", "DEMO_MIRROR_CLUE", "DEMO_MIRROR_SEEN", .42f, Art("fd-070")),
                Travel("DEMO_WC_BACK", "Cửa\nRa hành lang", "HALL_01", .08f));
            content.rooms[1].hotspots = content.rooms[1].hotspots.Concat(new[] {
                Travel("DEMO_HALL_DOWN", "Cầu thang\nXuống phòng khách", "DEMO_LIVING", .28f),
                Travel("DEMO_HALL_SHRINE", "Cuối hành lang\nPhòng thờ", "DEMO_SHRINE", .8f),
                Travel("DEMO_HALL_WC", "Nhà vệ sinh", "DEMO_BATHROOM", .51f)
            }).ToArray();
            map.bounds = new Rect(.29f,.2f,.18f,.22f);
            content.rooms[1].hotspots.First(h => h.id == "DEMO_HALL_DOWN").bounds = new Rect(.29f,.47f,.18f,.25f);
            var chaseStairs = Room("DEMO_CHASE_STAIRS", "Cầu thang trong bóng tối", "MÀN 2 · CHỌN SAI ĐƯỜNG = BAD END", new Color(.13f,.12f,.14f),
                Travel("DEMO_ESCAPE_UP", "Lên tầng 2\nTìm phòng thờ", "DEMO_CHASE_HALL", .06f),
                Travel("DEMO_ESCAPE_DOWN", "Xuống tầng 1\nChạy ra ngõ", "DEMO_BAD", .4f),
                Travel("DEMO_ESCAPE_HIDE", "Nấp dưới cầu thang", "DEMO_BAD", .73f));
            chaseStairs.chapter = 2;
            var chaseHall = Room("DEMO_CHASE_HALL", "Hành lang tầng 2", "MÀN 2 · TIẾNG DÉP ĐANG TỚI GẦN", new Color(.12f,.16f,.17f),
                Travel("DEMO_ESCAPE_SHRINE", "Chạy hết hành lang\nVào phòng thờ", "DEMO_NORMAL", .7f),
                Travel("DEMO_ESCAPE_BED", "Về phòng ngủ\nKhóa cửa lại", "DEMO_BAD", .08f));
            chaseHall.chapter = 2;
            chaseHall.hotspots[0].solvesPuzzle = "DEMO_NORMAL_END";
            var bad = Room("DEMO_BAD", "Không kịp tới nơi trú ẩn", "BAD END", new Color(.12f,.02f,.02f));
            bad.ending = StoryEnding.Bad;
            bad.description = "Nam chọn sai đường. Tiếng dép dừng ngay sau lưng. Khi ngoảnh lại, người phụ nữ đã ở sát bên.\n\nĐêm đầu tiên khép lại tại đây. Thử lại từ checkpoint và nhớ đường tới phòng thờ.";
            var normal = Room("DEMO_NORMAL", "Sống sót qua đêm đầu tiên", "KẾT THÚC THƯỜNG", new Color(.24f,.18f,.12f));
            normal.ending = StoryEnding.Good;
            normal.description = "Nam chạy qua ngưỡng phòng thờ. Ba người bạn theo vào. Người phụ nữ đứng ngoài cửa, không bước qua được.\n\nCả nhóm sống sót qua đêm. Sáng mai, họ sẽ tìm hiểu căn nhà này đã từng xảy ra chuyện gì.\n\nHẾT DEMO MÀN 1–2";
            content.rooms = content.rooms.Concat(new[] { living, shrine, bathroom, chaseStairs, chaseHall, bad, normal }).ToArray();
            content.initiallyUnlockedRooms = content.rooms.Select(r => r.id).ToArray();
            return content;
        }

        private static AdventureObjective Objective(string id, string text) => new AdventureObjective { afterPuzzle = id, text = text };
        private static ClueDefinition Clue(string id, string title, string text) => new ClueDefinition { id = id, title = title, body = text };
        private static AdventureRoom Room(string id, string title, string subtitle, Color color, params AdventureHotspot[] hotspots) =>
            new AdventureRoom { id = id, title = title, subtitle = subtitle, placeholderColor = color, hotspots = hotspots };
        private static AdventureHotspot Travel(string id, string label, string destination, float x, string required = "") =>
            new AdventureHotspot { id = id, label = label, kind = InteractionKind.Travel, destinationRoom = destination,
                bounds = new Rect(x,.28f,.2f,.4f), visualClass = "door", requiredPuzzle = required,
                lockedResponse = "Hãy khám phá và hoàn thành việc đang làm trước.", response = "Đã tới khu vực tiếp theo." };
        private static AdventureHotspot Read(string id, string label, string clue, string puzzle, float x, Sprite artwork) =>
            new AdventureHotspot { id = id, label = label, kind = InteractionKind.ReadClue, clueId = clue, solvesPuzzle = puzzle,
                bounds = new Rect(x,.27f,.23f,.4f), visualClass = "note", artwork = artwork, response = "Đã ghi manh mối vào sổ." };
        private static AdventureHotspot Corner(int number, string label, string previous, float x) =>
            new AdventureHotspot { id = "DEMO_CORNER_HOTSPOT_" + number, label = label, kind = InteractionKind.Inspect,
                bounds = new Rect(x,.3f,.2f,.4f), visualClass = "friend", visibleAfterPuzzle = previous,
                hiddenAfterPuzzle = "DEMO_CORNER_" + number, solvesPuzzle = "DEMO_CORNER_" + number,
                response = number == 4 ? "Nam chạm vào một bờ vai lạnh ngắt. Bật đèn: vẫn chỉ có bốn người. Ngoài cửa vang tiếng ru và tiếng dép kéo lê." : "Người được vỗ vai tiếp tục lượt kế tiếp." };
        private static Sprite Art(string id)
        {
            string path = Directory.GetFiles("Assets/FabricatedDawn/Art/AssetVault", "*.png", SearchOption.AllDirectories)
                .FirstOrDefault(p => new DirectoryInfo(Path.GetDirectoryName(p)).Name.StartsWith(id + "_", StringComparison.Ordinal));
            return path == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\','/'));
        }

        [MenuItem("Tools/Fabricated Dawn/Demo/Kiểm tra Prologue, tiến độ và checkpoint")]
        public static void Verify()
        {
            var content = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            if (content == null) throw new InvalidOperationException("Chuẩn bị demo trước.");
            var sandbox = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(SandboxPath);
            var report = ToolkitVerification.Run(sandbox);
            Check(report, "Demo starts without an ending and rejects early exit", () => {
                var session = new AdventureSession(content);
                Require(AdventureValidator.Errors(content).Count == 0);
                Require(content.plannedChapterCount == 8);
                Require(content.rooms.All(r => r.ending != StoryEnding.True));
                Require(content.rooms.Where(r => r.ending == StoryEnding.Good).All(r => r.chapter == 8));
                Require(session.Story.State.ending == StoryEnding.None && !session.Interact("HP_EXIT").succeeded);
            });
            Check(report, "Reaching shrine completes M2 without ending the eight-chapter demo", () => {
                var session = ReadyForChase(content);
                Act(session, "DEMO_ESCAPE_UP"); Act(session, "DEMO_ESCAPE_SHRINE");
                Require(session.CurrentRoom.id == "DEMO_M2_SHRINE" && session.Story.State.chapter == 2);
                Require(session.Story.State.ending == StoryEnding.None && !session.Completed);
            });
            Check(report, "Both wrong stair choices lead to Bad End", () => {
                foreach (var id in new[] { "DEMO_ESCAPE_DOWN", "DEMO_ESCAPE_HIDE" }) {
                    var session = ReadyForChase(content); Act(session,id);
                    Require(session.Story.State.ending == StoryEnding.Bad && !session.Completed);
                }
            });
            Check(report, "Wrong bedroom refuge leads to Bad End", () => {
                var session = ReadyForChase(content); Act(session,"DEMO_ESCAPE_UP"); Act(session,"DEMO_ESCAPE_BED");
                Require(session.Story.State.ending == StoryEnding.Bad);
            });
            Check(report, "Checkpoint restore recovers pre-chase inventory and room", () => {
                var session = ReadyForChase(content);
                var saved = session.Story.CaptureCheckpoint();
                Act(session,"DEMO_ESCAPE_DOWN"); session.Restore(saved);
                Require(session.Story.State.ending == StoryEnding.None && session.CurrentRoom.id == "DEMO_CHASE_STAIRS");
                Require(session.Story.State.inventory.Exists(i => i.id == "ITEM_SANDBOX_STONE"));
                Act(session,"DEMO_ESCAPE_UP"); Act(session,"DEMO_ESCAPE_SHRINE");
                Require(session.CurrentRoom.id == "DEMO_M2_SHRINE" && session.Story.State.ending == StoryEnding.None);
            });
            Check(report, "Terminal checkpoint roundtrip preserves ending", () => {
                var session = ReadyForChase(content); Act(session,"DEMO_ESCAPE_DOWN");
                var restored = new AdventureSession(content); restored.Restore(session.Story.CaptureCheckpoint());
                Require(restored.Story.State.ending == StoryEnding.Bad && !restored.Interact("HP_KEY").succeeded);
            });
            Check(report, "All demo artwork references resolve", () => {
                Require(content.items.All(i => i.icon != null));
                Require(Art("fd-004") != null && Art("fd-050") != null && Art("fd-081") != null);
            });
            Check(report, "Prologue prevents skipping dog and group dialogue", () => {
                var session = new AdventureSession(content);
                Require(session.CurrentRoom.id == PrologueSetup.FirstRoom);
                Require(!session.Interact("PROLOGUE_STREET_NEXT").succeeded);
                Act(session,"PROLOGUE_DOG"); Act(session,"PROLOGUE_STREET_NEXT");
                Require(!session.Interact("PROLOGUE_ALLEY_NEXT").succeeded);
                Act(session,"PROLOGUE_GROUP");
                Require(!session.Interact("PROLOGUE_ALLEY_NEXT").succeeded);
            });
            Check(report, "Prologue title leads to M1 with journal intact", () => {
                var session = new AdventureSession(content); PrologueSetup.PlayToChapterOne(session);
                Require(session.CurrentRoom.id == "ROOM_01" && session.Story.State.ending == StoryEnding.None);
                Require(session.Story.State.clues.Contains("PROLOGUE_DOG_CLUE"));
                Require(session.Story.State.solvedPuzzles.Contains("PROLOGUE_DONE"));
                Require(!session.Interact("PROLOGUE_BEGIN_CHAPTER_1").succeeded);
            });
            Check(report, "Mid-prologue checkpoint resumes at the correct scene", () => {
                var session = new AdventureSession(content); Act(session,"PROLOGUE_DOG"); Act(session,"PROLOGUE_STREET_NEXT");
                var restored = new AdventureSession(content); restored.Restore(session.Story.CaptureCheckpoint());
                Require(restored.CurrentRoom.id == "PROLOGUE_ALLEY" && restored.Story.State.clues.Count == 1);
                Require(!restored.Interact("PROLOGUE_ALLEY_NEXT").succeeded);
            });
            Directory.CreateDirectory("Assets/FabricatedDawn/Reports");
            File.WriteAllText("Assets/FabricatedDawn/Reports/story-demo-checks.json", JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            if (report.failed.Count > 0) throw new InvalidOperationException(string.Join("\n", report.failed));
            Debug.Log("STORY_DEMO_VERIFIED: " + report.passed.Count + " checks passed.");
        }

        private static AdventureSession ReadyForChase(AdventureDefinition content)
        {
            var s = new AdventureSession(content);
            PrologueSetup.PlayToChapterOne(s);
            Act(s,"HP_STONE"); Act(s,"HP_KEY"); Require(s.SelectItem("ITEM_SANDBOX_KEY"));
            Act(s,"HP_CABINET"); Act(s,"HP_NOTE"); Act(s,"HP_EXIT");
            Act(s,"DEMO_HALL_DOWN"); Act(s,"DEMO_BOWL_HOTSPOT"); Act(s,"DEMO_LIVING_UP");
            Act(s,"DEMO_HALL_SHRINE"); Act(s,"DEMO_SHRINE_INSPECT"); Act(s,"DEMO_SHRINE_BACK");
            Act(s,"DEMO_HALL_DOWN"); Act(s,"DEMO_GAME_BEGIN");
            while (s.ActiveDialogue != null) s.AdvanceDialogue();
            for (int i=1; i<=4; i++) Act(s,"DEMO_CORNER_HOTSPOT_" + i);
            Act(s,"DEMO_RUN"); return s;
        }
        private static void Act(AdventureSession session, string id) => Require(session.Interact(id).succeeded);
        private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("Demo assertion failed."); }
        private static void Check(VerificationReport report, string title, Action action)
        {
            try { action(); report.passed.Add(title); }
            catch (Exception ex) { report.failed.Add(title + ": " + ex.Message); }
        }
    }
}
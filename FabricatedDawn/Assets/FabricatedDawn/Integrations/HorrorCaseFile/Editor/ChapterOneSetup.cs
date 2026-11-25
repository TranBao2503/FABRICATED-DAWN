using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FabricatedDawn.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FabricatedDawn.CaseFile.Editor
{
    // GDD 5.2. The scene is separate so the existing Prologue remains available.
    public static class ChapterOneSetup
    {
        const string Data = "Assets/FabricatedDawn/Data/ChapterOne";
        const string ArtRoot = "Assets/FabricatedDawn/Art/ChapterOne";
        public const string ContentPath = Data + "/ChapterOne.asset";
        public const string ScenePath = "Assets/FabricatedDawn/Scenes/ChapterOne.unity";
        static ItemDefinition key, amulet;
        static readonly List<AdventureRoom> rooms = new List<AdventureRoom>();
        static readonly List<ClueDefinition> clues = new List<ClueDefinition>();
        static readonly List<DialogueDefinition> dialogues = new List<DialogueDefinition>();

        [MenuItem("Tools/Fabricated Dawn/Màn 1/Chuẩn bị và mở Những ngày ở trọ")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Dừng Play trước khi chuẩn bị màn.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Data);
            AssetDatabase.Refresh();
            ImportArt();
            key = Item("M1_KEY", "Chìa khóa tủ", "Chìa khóa tủ quần áo. Chọn trong túi rồi nhấn vào tủ.", "fd-022");
            amulet = Item("M1_AMULET", "Vật hộ thân của Nam", "Vật Nam luôn mang bên người. Ký hiệu quen thuộc, Nam chưa biết ý nghĩa của nó.", "fd-026");
            var content = AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            if (content == null) { content = ScriptableObject.CreateInstance<AdventureDefinition>(); AssetDatabase.CreateAsset(content, ContentPath); }
            content.contentId = "fabricated-dawn-chapter-one-v1";
            content.startRoomId = "M1_BEDROOM";
            content.isStoryDemo = true;
            content.plannedChapterCount = 8;
            content.completionPuzzleId = "";
            content.items = new[] { key, amulet };
            content.initialItems = new[] { amulet };
            rooms.Clear(); clues.Clear(); dialogues.Clear();
            BuildDays(); BuildNight(); BuildHandoff();
            content.rooms = rooms.ToArray();
            content.clues = clues.ToArray();
            content.dialogues = dialogues.ToArray();
            content.initiallyUnlockedRooms = content.rooms.Select(r => r.id).ToArray();
            content.initialObjective = "Dọn đồ: tìm chìa khóa trên bàn, chọn chìa trong túi rồi mở tủ.";
            content.objectiveUpdates = new[] {
                Obj("M1_CABINET", "Đọc lời nhắn trong tủ rồi xuống phòng khách gặp các bạn."),
                Obj("M1_NOTE", "Xuống phòng khách: nói chuyện với Bia, Băng, Bảo và xem mâm cơm."),
                Obj("M1_BOWL", "Đi quanh nhà: rửa mặt ở WC 2, xem kỹ gương và ghé phòng thờ riêng tầng 2."),
                Obj("M1_ROUTE", "Xem gương ở WC 2 và nói chuyện đủ ba người bạn. Xong thì về phòng ngủ nghỉ."),
                Obj("M1_NIGHT", "Nửa đêm: ra hành lang, quan sát bóng đỏ rồi đi vệ sinh."),
                Obj("M1_NIGHT_WC", "Trở lại hành lang. Quả bóng đã nằm trước cửa phòng ngủ?"),
                Obj("M1_BALLOON", "Về phòng ngủ. Những ngày ở trọ vẫn tiếp tục…"),
                Obj("M1_READY", "Một tối khác: nghe các bạn bàn chuyện lạ. Checkpoint được lưu trước Vỗ Vai Ma."),
                Obj("DEMO_GAME_START", "Màn 2: đi theo lượt qua bốn góc phòng."),
                Obj("DEMO_CORNER_4", "Có người ở góc trống. Chạy ra cầu thang!"),
                Obj("DEMO_M2_COMPLETE", "Đã tới phòng thờ. Phần tiếp theo của demo đang được phát triển.")
            };
            EditorUtility.SetDirty(content);
            if (!File.Exists(ScenePath) && !AssetDatabase.CopyAsset("Assets/FabricatedDawn/Scenes/StoryDemo.unity", ScenePath))
                throw new IOException("Thiếu scene mẫu StoryDemo.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var ui = UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if (ui == null) throw new InvalidOperationException("Scene thiếu giao diện.");
            ui.content = content;
            EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Verify();
            StoryDemoSetup.Verify();
            Debug.Log("CHAPTER_ONE_READY: " + ScenePath);
        }

        static void BuildDays()
        {
            Clue("M1_NOTE_CLUE", "Lời nhắn trong tủ", "Nam, dọn đồ xong thì xuống ăn nhé. Bia đang ở phòng khách, Băng lo bữa cơm, Bảo vừa đi mua đồ.\n\nNhà vốn là nhà ở của họ hàng bên nội, bà cho thuê rẻ để giúp sinh viên và nhờ trông nhà. Phòng thờ là phòng riêng ở tầng 2, đừng để đồ chắn cửa. — Bia");
            Clue("M1_BOWL_CLUE", "Bát cơm thứ năm", "Bốn người, nhưng trên mâm có thêm một chiếc bát nhỏ mẻ miệng. Nam nhìn kỹ hoa văn trên thành bát.\n\nVừa quay đi hỏi các bạn rồi nhìn lại, chiếc bát đã không còn ở chỗ cũ. Chưa ai giải thích được.");
            Clue("M1_MIRROR_CLUE", "Bóng phía sau trong gương", "Sau khi rửa mặt, Nam ngẩng lên thấy bóng một người phụ nữ sau lưng. Nam chưa nhận ra đó là ai.");
            Clue("M1_HAND_CLUE", "Dấu bàn tay nhỏ", "Nam quay lại: không có ai. Khi nhìn về gương lần nữa, sát mép dưới có một dấu bàn tay nhỏ. Bề mặt gương vẫn lạnh.");
            Clue("M1_ROUTE_CLUE", "Đường đến phòng thờ", "Tầng 1: phòng khách phía trước; phòng ngủ 1 bên trái; cầu thang bên phải; bếp và WC 1 phía sau, cửa phụ ra sân sau.\n\nTầng 2: hành lang giữa nhà; phòng ngủ 2 phía trước bên trái; PHÒNG THỜ RIÊNG phía trước bên phải; phòng ngủ 3 phía sau bên trái; WC 2 phía sau bên phải.\n\nPhòng thờ yên tĩnh khác thường. Ký hiệu dòng họ trên đồ thờ giống vật Nam mang bên mình. Chưa biết ý nghĩa hay tác dụng.");
            Clue("M1_DOG_CLUE", "Chó sủa đầu ngõ", "Lúc cả nhóm đi qua đầu ngõ, con chó bỗng sủa. Không rõ nó nhìn gì; Nam không quy tiếng sủa cho một người cụ thể.");
            TalkData("M1_BIA_TALK", "Bia", "Ở được mấy hôm mà cậu vẫn để đồ trong ba lô à? Dọn hết đi cho rộng.", "Bà họ hàng cho thuê rẻ, đổi lại mình nhớ trông nhà giúp bà. Đừng làm mất chìa khóa nữa đấy.");
            TalkData("M1_BANG_TALK", "Băng", "Hôm nay tớ nấu, mai tới lượt cậu nhé. Bếp ở sau nhà, đi qua lối cạnh cầu thang.", "Có bốn đứa thôi, đừng lấy dư bát. Ăn xong ai rảnh thì phụ rửa.");
            TalkData("M1_BAO_TALK", "Bảo", "Tớ ra hàng đầu ngõ mua thêm gói mì. Cậu cần gì thì đi cùng.", "Nhà vẫn chia phòng y như hồi chủ cũ ở. Trên tầng có phòng thờ riêng, vào thì giữ yên tĩnh nhé.");
            var pick = H("M1_KEY_PICK", "Chìa khóa trên bàn", InteractionKind.Pickup, .735f,.475f,.065f,.07f, "Nhặt được chìa khóa. Mở Túi đồ hoặc chọn chìa ở thanh dưới rồi nhấn vào tủ.");
            pick.item = key; pick.artwork = key.icon;
            var cabinet = H("M1_CABINET_USE", "Tủ quần áo · dùng chìa khóa", InteractionKind.UseItem, .46f,.19f,.18f,.55f, "Khóa bật mở. Có lời nhắn bên trong tủ.");
            cabinet.item = key; cabinet.solvesPuzzle = "M1_CABINET"; cabinet.hiddenAfterPuzzle = "M1_CABINET";
            var note = Read("M1_NOTE_READ", "Lời nhắn trong tủ", "M1_NOTE_CLUE", "M1_NOTE", .515f,.38f,.10f,.14f, Art("fd-010"));
            note.visibleAfterPuzzle = "M1_CABINET";
            var outBed = Go("M1_BED_OUT", "Ra hành lang →", "M1_HALL", .88f,.32f,.10f,.40f);
            outBed.requiredPuzzle = "M1_NOTE"; outBed.lockedResponse = "Dọn đồ và đọc lời nhắn trong tủ trước đã.";
            var sleep = Go("M1_SLEEP", "Nghỉ tới khuya", "M1_BED_NIGHT", .08f,.67f,.21f,.075f);
            sleep.visibleAfterPuzzle="M1_ROUTE";
            sleep.requiredPuzzles = new[] { "M1_BOWL", "M1_HAND", "M1_ROUTE" };
            sleep.requiredEvents = new[] { "dialogue:M1_BIA_TALK", "dialogue:M1_BANG_TALK", "dialogue:M1_BAO_TALK" };
            sleep.lockedResponse = "Trước khi nghỉ: gặp đủ ba bạn, xem mâm cơm, rửa mặt và xem kỹ gương ở WC 2, ghé phòng thờ.";
            sleep.solvesPuzzle = "M1_NIGHT"; sleep.checkpointAfter = true;
            var r = Room("M1_BEDROOM", "Phòng ngủ 2 · phòng của nhóm", "bedroom", pick,cabinet,note,outBed,sleep,
                Inspect("M1_PACK", "Sắp lại giáo trình", .16f,.54f,.12f,.16f,"Nam xếp giáo trình lên đầu giường, cất quần áo vào tủ. Bên ngoài, tiếng quạt đều đều.", Art("fd-031")));
            Room("M1_HALL", "Hành lang tầng 2", "hall",
                Go("M1_HALL_BED", "← Phòng ngủ 2", "M1_BEDROOM", .015f,.35f,.11f,.40f),
                Go("M1_HALL_SHRINE", "Phòng thờ riêng →", "M1_SHRINE", .90f,.31f,.095f,.42f),
                Go("M1_HALL_WC", "WC 2", "M1_WC2", .755f,.27f,.09f,.39f),
                Go("M1_HALL_DOWN", "↓ Xuống tầng 1", "M1_LIVING", .68f,.72f,.20f,.12f),
                Inspect("M1_BED3", "Phòng ngủ 3", .235f,.23f,.17f,.45f,"Phòng ngủ phía sau đang đóng. Lối giữa nhà vẫn đủ thoáng để đi lại."),
                Inspect("M1_SOUND", "Lắng nghe", .47f,.24f,.16f,.12f,"[Phụ đề âm thanh] Một tiếng ho khan, rồi tiếng dép lê. Nam ngẩng lên nhưng không thấy ai bước ra."));
            var bia = Talk("M1_BIA", "Nói chuyện với Bia", "M1_BIA_TALK", .08f,.43f,.16f,.17f);
            var bang = Talk("M1_BANG", "Nói chuyện với Băng", "M1_BANG_TALK", .28f,.43f,.16f,.17f);
            var bao = Talk("M1_BAO", "Nói chuyện với Bảo", "M1_BAO_TALK", .48f,.43f,.16f,.17f);
            Actor(bia,"bia",.22f,.30f);
            Actor(bang,"bang",.52f,.59f);
            Actor(bao,"bao",.635f,.70f);
            var bowl = Read("M1_BOWL_LOOK", "Chiếc bát nhỏ thứ năm · xem hoa văn", "M1_BOWL_CLUE", "M1_BOWL", .525f,.735f,.055f,.070f, Art("fd-004"));
            bowl.hiddenAfterPuzzle = "M1_BOWL"; bowl.checkpointAfter = true;
            Room("M1_LIVING", "Phòng khách · bữa cơm bốn người", "living", bia,bang,bao,bowl,
                Inspect("M1_MEAL", "Mâm cơm", .39f,.69f,.20f,.16f,"Bốn bộ bát đũa được bày cho bốn người. Có thứ gì nhỏ hơn nằm cạnh chúng?"),
                Go("M1_LIVING_UP", "↑ Cầu thang lên tầng 2", "M1_HALL", .74f,.30f,.18f,.38f),
                Go("M1_LIVING_KITCHEN", "Bếp + ăn →", "M1_KITCHEN", .53f,.25f,.11f,.32f),
                Go("M1_LIVING_STREET", "← Ra ngõ", "M1_ALLEY", .015f,.69f,.12f,.10f),
                Inspect("M1_BED1", "Phòng ngủ 1", .275f,.255f,.115f,.36f,"Phòng ngủ tầng 1 nằm bên trái lối đi. Cửa đang đóng, không cần vào lúc này."));
            // Meal layer must receive clicks before the broad tray region.
            var living = rooms.Last();
            var meal = new List<AdventureHotspot>();
            for(var i=0;i<4;i++) meal.Add(Inspect("M1_RICE_"+i,"Bát cơm của nhóm",.405f+(i%2)*.065f,.72f+(i/2)*.064f,.05f,.065f,"Bát cơm bình thường. Bốn người, bốn chiếc bát.",BG("rice-bowl")));
            living.hotspots = living.hotspots.Where(h => h != bowl && h != bia && h != bang && h != bao)
                .Concat(new[]{bia,bang,bao}).Concat(meal).Concat(new[] { bowl }).ToArray();
            Room("M1_KITCHEN", "Bếp và chỗ ăn · tầng 1", "kitchen",
                Inspect("M1_COOK", "Phụ chuẩn bị bữa ăn", .19f,.40f,.20f,.18f,"Nam kiểm tra bếp, đổ nước vào ấm. Ngày mai tới lượt mình nấu. Bếp nhà dân vẫn dùng bình thường."),
                Go("M1_KITCHEN_BACK", "← Phòng khách", "M1_LIVING", .01f,.32f,.10f,.35f),
                Go("M1_KITCHEN_YARD", "Cửa phụ · sân sau", "M1_YARD", .49f,.23f,.20f,.40f),
                Go("M1_KITCHEN_WC", "WC 1", "M1_WC1", .81f,.27f,.12f,.43f));
            Room("M1_WC1", "Nhà vệ sinh 1 · tầng 1", "bathroom",
                Inspect("M1_WC1_TAP", "Vòi nước", .43f,.49f,.18f,.18f,"Vòi nước vẫn hoạt động bình thường."),
                Go("M1_WC1_BACK", "← Về bếp", "M1_KITCHEN", .01f,.36f,.13f,.35f));
            var wash = Inspect("M1_WASH", "Rửa mặt", .43f,.49f,.18f,.18f,"Nam cúi xuống rửa mặt, rồi ngẩng nhìn vào gương. Có bóng ai phía sau?");
            wash.solvesPuzzle = "M1_WASHED"; wash.hiddenAfterPuzzle = "M1_WASHED";
            var mirror = Read("M1_MIRROR", "Bóng người phụ nữ trong gương", "M1_MIRROR_CLUE", "M1_MIRROR", .46f,.18f,.13f,.27f, BG("woman-reflection"));
            mirror.visibleAfterPuzzle = "M1_WASHED"; mirror.hiddenAfterPuzzle = "M1_TURNED";
            var turn = Inspect("M1_TURN", "Quay lại nhìn phía sau", .68f,.39f,.20f,.12f,"Phía sau trống không. Khi Nam quay về gương, sát chân gương hiện một dấu tay nhỏ.");
            turn.visibleAfterPuzzle = "M1_MIRROR"; turn.hiddenAfterPuzzle = "M1_TURNED"; turn.solvesPuzzle = "M1_TURNED";
            var hand = Read("M1_HAND", "Dấu bàn tay nhỏ dưới gương", "M1_HAND_CLUE", "M1_HAND", .54f,.39f,.055f,.07f, Art("fd-070")); hand.visibleAfterPuzzle = "M1_TURNED";
            Room("M1_WC2", "Nhà vệ sinh 2 · tầng 2", "bathroom", wash,mirror,turn,hand,
                Go("M1_WC2_BACK", "← Ra hành lang", "M1_HALL", .01f,.36f,.13f,.35f));
            Room("M1_SHRINE", "Phòng thờ riêng · tầng 2", "shrine",
                Read("M1_SHRINE_SIGN", "Ký hiệu dòng họ · xem kỹ", "M1_ROUTE_CLUE", "M1_ROUTE", .48f,.37f,.065f,.10f, amulet.icon),
                Inspect("M1_SHRINE_SILENCE", "Lắng nghe trong phòng", .22f,.58f,.21f,.12f,"Căn phòng yên tĩnh đến lạ. Tiếng quạt và tiếng người bên ngoài dường như xa hẳn."),
                Go("M1_SHRINE_BACK", "Ra hành lang →", "M1_HALL", .87f,.29f,.12f,.43f));
            Room("M1_YARD", "Sân sau · sau cửa phụ", "yard",
                Inspect("M1_LAUNDRY", "Phơi áo sau nhà", .25f,.35f,.28f,.25f,"Nam giũ áo rồi mắc lên dây. Gió lùa qua chậu cây và mái tôn. Đây vẫn chỉ là một ngày ở trọ bình thường."),
                Go("M1_YARD_BACK", "← Cửa phụ về bếp", "M1_KITCHEN", .79f,.39f,.18f,.18f));
            Room("M1_ALLEY", "Hàng tạp hóa đầu ngõ", "alley",
                Read("M1_DOG", "Tiếng chó sủa đầu ngõ", "M1_DOG_CLUE", "M1_DOG", .48f,.42f,.15f,.24f, null),
                Inspect("M1_SHOP", "Mua ít mì và đồ dùng", .25f,.35f,.17f,.25f,"Nam và các bạn mua thêm mấy gói mì, xà phòng. [Phụ đề: chó sủa phía đầu ngõ.] Không ai hiểu vì sao nó sủa."),
                Go("M1_ALLEY_BACK", "Về nhà →", "M1_LIVING", .84f,.37f,.15f,.30f));
        }

        static void BuildNight()
        {
            Clue("M1_BALLOON_CLUE", "Quả bóng đỏ", "Giữa đêm đi vệ sinh, Nam thấy một quả bóng đỏ lơ lửng ngoài hành lang, sợi dây căng như có ai giữ.\n\nLúc quay về, quả bóng nằm trước phòng ngủ. Cảnh này chỉ xảy ra một lần. Không thấy người cầm bóng.");
            Room("M1_BED_NIGHT", "Phòng ngủ 2 · nửa đêm", "bedroom-night",
                Go("M1_NIGHT_OUT", "Ra ngoài đi vệ sinh →", "M1_HALL_NIGHT", .88f,.32f,.10f,.40f));
            var balloon = Inspect("M1_FLOAT", "Bóng đỏ lơ lửng", .39f,.28f,.13f,.40f,"Quả bóng lơ lửng. Sợi dây căng thẳng xuống dưới như có một bàn tay đang giữ.", Art("fd-007"));
            balloon.solvesPuzzle = "M1_FLOAT";
            var wc = Go("M1_NIGHT_TO_WC", "WC 2", "M1_WC_NIGHT", .755f,.27f,.09f,.39f);
            wc.requiredPuzzle = "M1_FLOAT"; wc.lockedResponse = "Có thứ gì màu đỏ ngay giữa hành lang. Nhìn kỹ một chút…";
            Room("M1_HALL_NIGHT", "Hành lang tầng 2 · đêm", "hall-night", balloon,wc,
                Inspect("M1_NIGHT_NOISE", "Lắng nghe", .55f,.19f,.18f,.12f,"[Phụ đề âm thanh] Tiếng dép lê, tiếng chân nhỏ và ba tiếng gõ tủ. Mọi thứ lại im bặt."));
            var nightWash = Inspect("M1_NIGHT_WASH", "Dùng WC rồi rửa tay", .43f,.49f,.18f,.18f,"Nước chảy nhỏ giọt. Trong gương lúc này không có ai khác.");
            nightWash.solvesPuzzle = "M1_NIGHT_WC";
            var back = Go("M1_NIGHT_RETURN", "← Ra hành lang", "M1_HALL_RETURN", .01f,.36f,.13f,.35f); back.requiredPuzzle = "M1_NIGHT_WC";
            Room("M1_WC_NIGHT", "WC 2 · nửa đêm", "bathroom-night", nightWash,back);
            var fallen = Read("M1_BALLOON_RETURN", "Bóng đỏ nằm trước cửa phòng", "M1_BALLOON_CLUE", "M1_BALLOON", .10f,.67f,.12f,.16f, Art("fd-007"));
            fallen.hiddenAfterPuzzle = "M1_BALLOON";
            var returnBed = Go("M1_RETURN_BED", "← Vào phòng ngủ", "M1_LATER", .015f,.35f,.11f,.40f);
            returnBed.requiredPuzzle = "M1_BALLOON"; returnBed.checkpointAfter = true;
            Room("M1_HALL_RETURN", "Trước cửa phòng ngủ · đêm", "hall-night", fallen,returnBed);
            var later = Room("M1_LATER", "Những ngày sau đó", "bedroom-night",
                Go("M1_NEXT_EVENING", "Một tối khác · xuống gặp các bạn →", "M1_READY", .31f,.66f,.38f,.12f));
            later.narration = "NHỮNG NGÀY Ở TRỌ\n\nLớp học, bữa cơm, chuyện vặt.\nNhững điều kỳ lạ vẫn chưa có lời giải.";
            later.hotspots[0].solvesPuzzle = "M1_READY"; later.hotspots[0].checkpointAfter = true;
        }

        static void BuildHandoff()
        {
            TalkData("M1_END_TALK", "Bia · Băng · Bảo · Nam", "Băng: Hôm nọ ai lấy thêm cái bát nhỏ thế? Tớ bày có bốn cái thôi.", "Nam: Tớ còn thấy một quả bóng ngoài hành lang. Lúc đi thì nó lơ lửng, lúc về lại nằm trước cửa.", "Bảo: Ở nhà cũ, nghe tiếng động là bình thường mà. Hay thử Vỗ Vai Ma cho vui?", "Bia: Bốn đứa, bốn góc. Được thì lát nữa dọn phòng khách rồi chơi.", "Cả nhóm cười, đồng ý. Không ai biết nhà từng có người chết.");
            var next = Go("M1_TO_M2", "Bắt đầu Màn 2 · Vỗ Vai Ma →", "DEMO_LIVING", .81f,.66f,.17f,.12f);
            next.requiredEvent = "dialogue:M1_END_TALK";
            next.response = "Màn 2 · Người thứ năm. Một trò đùa trong căn phòng đóng cửa.";
            next.checkpointAfter = false;
            Room("M1_READY", "Phòng khách · một tối khác", "living-night",
                Talk("M1_END_GROUP", "Ngồi cùng các bạn · nhắc chuyện lạ", "M1_END_TALK", .10f,.41f,.38f,.21f), next);
            var ready=rooms.Last();
            Actor(ready.hotspots[0],"bia",.22f,.30f);
            var bang=Talk("M1_END_BANG","Băng · nghe chuyện lạ","M1_END_TALK",.59f,.30f,.10f,.64f);Actor(bang,"bang",.52f,.59f);
            var bao=Talk("M1_END_BAO","Bảo · nghe chuyện lạ","M1_END_TALK",.70f,.30f,.10f,.64f);Actor(bao,"bao",.635f,.70f);
            ready.hotspots=ready.hotspots.Concat(new[]{bang,bao}).ToArray();
            // Preserve the existing M2 prototype, but introduce it after the complete M1.
            var original = AssetDatabase.LoadAssetAtPath<AdventureDefinition>("Assets/FabricatedDawn/Data/Demo/StoryDemo.asset");
            if (original == null) throw new InvalidOperationException("Thiếu demo gốc để nối Màn 2.");
            var copy = UnityEngine.Object.Instantiate(original);
            foreach (var id in new[] { "DEMO_LIVING", "DEMO_CHASE_STAIRS", "DEMO_CHASE_HALL", "DEMO_BAD", "DEMO_M2_SHRINE" })
            {
                var room = copy.Room(id);
                if (room == null) throw new InvalidOperationException("Thiếu phòng M2: " + id);
                room.chapter = 2;
                room.subtitle = "MÀN 2 · NGƯỜI THỨ NĂM";
                if (id == "DEMO_LIVING") {
                    room.title = "Phòng khách · Vỗ Vai Ma"; room.presentBackground = BG("living-night");
                    room.hotspots = room.hotspots.Where(h => h.id != "DEMO_BOWL_HOTSPOT" && h.id != "DEMO_LIVING_UP").ToArray();
                    var begin = room.hotspots.First(h => h.id == "DEMO_GAME_BEGIN"); begin.requiredPuzzle = "";
                    // A loaded checkpoint during dialogue must not skip the game's introduction.
                    foreach (var corner in room.hotspots.Where(h => h.id.StartsWith("DEMO_CORNER_HOTSPOT_")))
                        corner.requiredEvent = "dialogue:DEMO_GAME_DIALOGUE";
                }
                if (id == "DEMO_CHASE_STAIRS") room.presentBackground = BG("living-night");
                if (id == "DEMO_CHASE_HALL") room.presentBackground = BG("hall-night");
                if (id == "DEMO_M2_SHRINE") room.presentBackground = BG("shrine");
                rooms.Add(room);
            }
            dialogues.Add(new DialogueDefinition { id = "DEMO_GAME_DIALOGUE", speaker = "Bia", lines = new[] {
                "Mỗi đứa đứng một góc. Tắt đèn rồi người đầu đi dọc tường, vỗ vai người kế tiếp.",
                "Ai được vỗ vai thì tiếp tục. Đến góc trống thì dừng lại. Chỉ có bốn đứa mình thôi mà." } });
            UnityEngine.Object.DestroyImmediate(copy);
        }

        static AdventureRoom Room(string id, string title, string bg, params AdventureHotspot[] hotspots)
        {
            var r = new AdventureRoom { id=id, title=title, chapter=1, subtitle="MÀN 1 · NHỮNG NGÀY Ở TRỌ", presentBackground=BG(bg), hotspots=hotspots, placeholderColor=new Color(.23f,.24f,.21f) };
            rooms.Add(r); return r;
        }
        static AdventureHotspot H(string id,string label,InteractionKind kind,float x,float y,float w,float h,string response) =>
            new AdventureHotspot { id=id,label=label,kind=kind,bounds=new Rect(x,y,w,h),response=response,sceneHotspot=true };
        static AdventureHotspot Go(string id,string label,string room,float x,float y,float w,float h) {
            var a=H(id,label,InteractionKind.Travel,x,y,w,h,"Đã tới khu vực tiếp theo."); a.destinationRoom=room; return a;
        }
        static AdventureHotspot Inspect(string id,string label,float x,float y,float w,float h,string text,Sprite art=null) {
            var a=H(id,label,InteractionKind.Inspect,x,y,w,h,text); a.artwork=art; return a;
        }
        static AdventureHotspot Read(string id,string label,string clue,string puzzle,float x,float y,float w,float h,Sprite art) {
            var a=H(id,label,InteractionKind.ReadClue,x,y,w,h,"Đã xem kỹ và ghi lại trong sổ manh mối."); a.clueId=clue; a.solvesPuzzle=puzzle; a.artwork=art; return a;
        }
        static AdventureHotspot Talk(string id,string label,string dialogue,float x,float y,float w,float h) {
            var a=H(id,label,InteractionKind.Talk,x,y,w,h,"Cuộc trò chuyện vẫn tiếp tục."); a.dialogueId=dialogue; return a;
        }
        static void Actor(AdventureHotspot h,string name,float artX,float hitX) {
            // Character designs are on hold. Keep only explicit dialogue buttons.
            int slot = name == "bia" ? 0 : name == "bang" ? 1 : 2;
            h.artwork = null;
            h.artworkBounds = default;
            h.bounds = new Rect(.04f + slot * .21f, .56f, .19f, .09f);
            h.sceneHotspot = false;
        }
        static void Clue(string id,string title,string body) => clues.Add(new ClueDefinition { id=id,title=title,body=body });
        static void TalkData(string id,string speaker,params string[] lines) => dialogues.Add(new DialogueDefinition { id=id,speaker=speaker,lines=lines });
        static AdventureObjective Obj(string id,string text) => new AdventureObjective { afterPuzzle=id,text=text };
        static Sprite BG(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + name + ".png");
        static Sprite Art(string id) {
            var p=Directory.GetFiles("Assets/FabricatedDawn/Art/AssetVault", "*.png", SearchOption.AllDirectories)
                .FirstOrDefault(f => new DirectoryInfo(Path.GetDirectoryName(f)).Name.StartsWith(id+"_",StringComparison.Ordinal));
            return p == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/'));
        }
        static ItemDefinition Item(string id,string title,string purpose,string art) {
            var path=Data+"/"+id+".asset"; var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if(item==null) { item=ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(item,path); }
            item.id=id;item.displayName=title;item.purpose=purpose;item.icon=Art(art);item.uses=1;
            EditorUtility.SetDirty(item);return item;
        }
        static void ImportArt() {
            if(!Directory.Exists(ArtRoot)) return;
            foreach(var file in Directory.GetFiles(ArtRoot,"*.png")) {
                var path=file.Replace('\\','/'); var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null) continue;
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            }
        }

        [MenuItem("Tools/Fabricated Dawn/Màn 1/Kiểm tra gameplay và checkpoint")]
        public static void Verify()
        {
            var c=AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            var report=ToolkitVerification.Run(AssetDatabase.LoadAssetAtPath<AdventureDefinition>("Assets/FabricatedDawn/Data/Sandbox/ToolkitAdventure.asset"));
            Check(report,"Content validates; M1 skips Prologue without early endings",()=> {
                var errors=AdventureValidator.Errors(c); Require(errors.Count==0,string.Join("; ",errors));
                Require(c.startRoomId=="M1_BEDROOM" && c.plannedChapterCount==8);
                Require(c.rooms.Where(r=>r.chapter==1).All(r=>r.ending==StoryEnding.None));
                Require(c.rooms.All(r=>r.ending!=StoryEnding.True && r.ending!=StoryEnding.Good));
            });
            Check(report,"Default amulet; wrong item cannot open cabinet or consume it",()=> {
                var s=new AdventureSession(c); Require(s.Story.State.inventory.Count==1);
                Require(s.SelectItem("M1_AMULET")); Require(!s.Interact("M1_CABINET_USE").succeeded);
                Require(s.Story.State.inventory.Exists(i=>i.id=="M1_AMULET"));
            });
            Check(report,"Key pickup is one shot; note unlocks domestic exploration",()=> {
                var s=new AdventureSession(c); Require(!s.Interact("M1_BED_OUT").succeeded);
                Act(s,"M1_KEY_PICK");Require(!s.Interact("M1_KEY_PICK").succeeded);
                Require(s.SelectItem("M1_KEY"));Act(s,"M1_CABINET_USE");Act(s,"M1_NOTE_READ");Act(s,"M1_BED_OUT");
            });
            Check(report,"Separate shrine matches approved two-storey residential layout",()=> {
                Require(c.Room("M1_SHRINE")!=null && c.Room("M1_SHRINE")!=c.Room("M1_HALL"));
                Require(c.Room("M1_HALL").hotspots.Any(h=>h.destinationRoom=="M1_SHRINE"));
                Require(c.Room("M1_WC1")!=null && c.Room("M1_WC2")!=null && c.Room("M1_KITCHEN")!=null);
                Require(c.Clue("M1_ROUTE_CLUE").body.Contains("PHÒNG THỜ RIÊNG"));
            });
            Check(report,"Mirror requires washing, looking, turning; hand appears afterward",()=> {
                var s=Explore(c); Act(s,"M1_HALL_WC");
                Require(!s.Interact("M1_HAND").succeeded);Act(s,"M1_WASH");
                Require(!s.Interact("M1_TURN").succeeded);Act(s,"M1_MIRROR");Act(s,"M1_TURN");
                Require(!s.Interact("M1_MIRROR").succeeded);Act(s,"M1_HAND");
            });
            Check(report,"Mandatory evidence cannot be skipped before the night",()=> {
                var s=new AdventureSession(c);Require(!s.Interact("M1_SLEEP").succeeded);
                s=Explore(c);Act(s,"M1_HALL_BED");Require(!s.Interact("M1_SLEEP").succeeded);
            });
            Check(report,"Bowl disappears after examining and survives checkpoint restore",()=> {
                var s=Explore(c);Act(s,"M1_HALL_DOWN");Act(s,"M1_BOWL_LOOK");
                var saved=s.Story.CaptureCheckpoint(); var loaded=new AdventureSession(c);loaded.Restore(saved);
                Require(!loaded.Interact("M1_BOWL_LOOK").succeeded && loaded.Story.State.clues.Contains("M1_BOWL_CLUE"));
            });
            Check(report,"Complete M1 records clues, one-shot balloon, ends at pre-game checkpoint",()=> {
                var s=Complete(c);Require(s.CurrentRoom.id=="M1_READY" && s.Story.State.chapter==1 && !s.Completed);
                Require(s.Story.State.ending==StoryEnding.None && s.Story.State.world==WorldPhase.Present);
                foreach(var id in new[]{"M1_BOWL_CLUE","M1_HAND_CLUE","M1_BALLOON_CLUE","M1_ROUTE_CLUE"})Require(s.Story.State.clues.Contains(id));
                Require(!s.Interact("M1_TO_M2").succeeded);
                var loaded=new AdventureSession(c);loaded.Restore(s.Story.CaptureCheckpoint());
                Require(loaded.CurrentRoom.id=="M1_READY" && !loaded.Interact("M1_TO_M2").succeeded);
                Act(loaded,"M1_END_GROUP");while(loaded.ActiveDialogue!=null)loaded.AdvanceDialogue();Act(loaded,"M1_TO_M2");
                Require(loaded.Story.State.chapter==2 && loaded.Story.State.ending==StoryEnding.None);
            });
            Check(report,"Night checkpoint retains balloon phase; return cannot replay first encounter",()=> {
                var s=Night(c);Act(s,"M1_NIGHT_OUT");Act(s,"M1_FLOAT");Act(s,"M1_NIGHT_TO_WC");Act(s,"M1_NIGHT_WASH");Act(s,"M1_NIGHT_RETURN");
                var loaded=new AdventureSession(c);loaded.Restore(s.Story.CaptureCheckpoint());
                Require(loaded.CurrentRoom.id=="M1_HALL_RETURN" && !loaded.Interact("M1_FLOAT").succeeded);
                Act(loaded,"M1_BALLOON_RETURN");Require(!loaded.Interact("M1_BALLOON_RETURN").succeeded);
            });
            Check(report,"Painted backgrounds and inventory resolve",()=> {
                Require(c.items.All(i=>i.icon!=null));
                Require(c.rooms.Where(r=>r.chapter==1).All(r=>r.presentBackground!=null));
                foreach(var name in new[]{"bia","bang","bao","rice-bowl","woman-reflection"})Require(BG(name)!=null,name);
            });
            Check(report,"Friends can be met in any order; unfinished dialogue cannot count as meeting",()=> {
                var s=Explore(c);Act(s,"M1_HALL_DOWN");
                foreach(var id in new[]{"M1_BAO","M1_BIA","M1_BANG"}){Act(s,id);while(s.ActiveDialogue!=null)s.AdvanceDialogue();}
                foreach(var id in new[]{"M1_BIA_TALK","M1_BANG_TALK","M1_BAO_TALK"})Require(s.Story.State.completedEvents.Contains("dialogue:"+id));
                var fresh=Explore(c);Act(fresh,"M1_HALL_DOWN");Act(fresh,"M1_BIA");
                var restored=new AdventureSession(c);restored.Restore(fresh.Story.CaptureCheckpoint());
                Require(!restored.Story.State.completedEvents.Contains("dialogue:M1_BIA_TALK"));Act(restored,"M1_BIA");
            });
            Check(report,"Character portraits are disabled and dialogue buttons do not cover M2 handoff",()=> {
                var ready=c.Room("M1_READY");var exit=ready.hotspots.First(h=>h.id=="M1_TO_M2");
                foreach(var h in ready.hotspots.Where(h=>h.kind==InteractionKind.Talk))
                    Require(!h.bounds.Overlaps(exit.bounds) && h.artwork == null && h.artworkBounds.width == 0);
            });
            Check(report,"M2 handoff preserves Bad End and shrine checkpoint without normal ending",()=> {
                var s=Complete(c);Act(s,"M1_END_GROUP");while(s.ActiveDialogue!=null)s.AdvanceDialogue();Act(s,"M1_TO_M2");
                Act(s,"DEMO_GAME_BEGIN"); while(s.ActiveDialogue!=null)s.AdvanceDialogue();
                for(var n=1;n<=4;n++)Act(s,"DEMO_CORNER_HOTSPOT_"+n);Act(s,"DEMO_RUN");
                var saved=s.Story.CaptureCheckpoint();Act(s,"DEMO_ESCAPE_DOWN");Require(s.Story.State.ending==StoryEnding.Bad);
                s.Restore(saved);Act(s,"DEMO_ESCAPE_UP");Act(s,"DEMO_ESCAPE_SHRINE");
                Require(s.CurrentRoom.id=="DEMO_M2_SHRINE" && s.Story.State.ending==StoryEnding.None && !s.Completed);
            });
            Directory.CreateDirectory("Assets/FabricatedDawn/Reports");
            File.WriteAllText("Assets/FabricatedDawn/Reports/chapter-one-checks.json",JsonUtility.ToJson(report,true));
            AssetDatabase.Refresh();
            if(report.failed.Count>0)throw new InvalidOperationException(string.Join("\n",report.failed));
            Debug.Log("CHAPTER_ONE_VERIFIED: "+report.passed.Count+" passed, 0 failed.");
        }
        static AdventureSession Explore(AdventureDefinition c) {
            var s=new AdventureSession(c);Act(s,"M1_KEY_PICK");s.SelectItem("M1_KEY");Act(s,"M1_CABINET_USE");Act(s,"M1_NOTE_READ");Act(s,"M1_BED_OUT");return s;
        }
        static AdventureSession Night(AdventureDefinition c) {
            var s=Explore(c);Act(s,"M1_HALL_DOWN");
            foreach(var id in new[]{"M1_BIA","M1_BANG","M1_BAO"}){Act(s,id);while(s.ActiveDialogue!=null)s.AdvanceDialogue();}
            Act(s,"M1_BOWL_LOOK");Act(s,"M1_LIVING_UP");Act(s,"M1_HALL_WC");Act(s,"M1_WASH");Act(s,"M1_MIRROR");Act(s,"M1_TURN");Act(s,"M1_HAND");Act(s,"M1_WC2_BACK");
            Act(s,"M1_HALL_SHRINE");Act(s,"M1_SHRINE_SIGN");Act(s,"M1_SHRINE_BACK");Act(s,"M1_HALL_BED");Act(s,"M1_SLEEP");return s;
        }
        static AdventureSession Complete(AdventureDefinition c) {
            var s=Night(c);foreach(var id in new[]{"M1_NIGHT_OUT","M1_FLOAT","M1_NIGHT_TO_WC","M1_NIGHT_WASH","M1_NIGHT_RETURN","M1_BALLOON_RETURN","M1_RETURN_BED","M1_NEXT_EVENING"})Act(s,id);return s;
        }
        static void Act(AdventureSession s,string id){var r=s.Interact(id);Require(r.succeeded,id+": "+r.message);}
        static void Require(bool condition,string message="M1 assertion failed"){if(!condition)throw new InvalidOperationException(message);}
        static void Check(VerificationReport r,string name,Action test){try{test();r.passed.Add(name);}catch(Exception e){r.failed.Add(name+": "+e.Message);}}
    }
}

using System;
using System.Linq;
using UnityEngine;

namespace FabricatedDawn.CaseFile.Editor
{
    // Draft prologue approved for implementation. Final art and voice can replace the placeholders.
    public static class PrologueSetup
    {
        public const string FirstRoom = "PROLOGUE_STREET";

        public static void Apply(AdventureDefinition content)
        {
            if (content.Room(FirstRoom) != null) return;
            content.contentId = "fabricated-dawn-eight-chapter-demo-v2";
            content.plannedChapterCount = 8;
            content.startRoomId = FirstRoom;
            content.initialObjective = "Prologue · Cùng nhóm bạn trở về nhà trọ.";
            content.completionPuzzleId = "";
            content.completedObjective = "Hoàn tất demo Fabricated Dawn.";
            content.objectiveUpdates = new[] {
                Objective("PROLOGUE_DOG_SEEN", "Theo các bạn vào ngách. Con chó vẫn sủa phía sau."),
                Objective("PROLOGUE_GROUP_TALKED", "Cùng nhóm trở về căn nhà ở cuối ngách."),
                Objective("PROLOGUE_DOOR_SEEN", "Bước qua cửa nhà."),
                Objective("PROLOGUE_DONE", "Màn 1 · Tìm chìa khóa, mở tủ và đọc lời nhắn trước khi ra khỏi phòng.")
            }.Concat(content.objectiveUpdates).Concat(new[] {
                Objective("DEMO_M2_COMPLETE", "Màn 2 hoàn tất. Tiếp theo là Màn 3 — Chuyện nhà chủ cũ (đang phát triển).")
            }).ToArray();
            content.clues = content.clues.Concat(new[] {
                new ClueDefinition { id = "PROLOGUE_DOG_CLUE", title = "Tiếng chó đầu ngõ",
                    body = "[Chó sủa liên hồi]\n\nCả nhóm vừa đi ngang đầu ngõ thì con chó bật dậy sủa. Nam ngoái nhìn, nhưng không rõ nó đang nhìn ai. Bia gọi mọi người đi tiếp." }
            }).ToArray();
            content.dialogues = content.dialogues.Concat(new[] {
                new DialogueDefinition { id = "PROLOGUE_GROUP_DIALOGUE", speaker = "Bia · Băng · Bảo",
                    lines = new[] {
                        "Bia: Lần nào đi qua đây nó cũng sủa. Thôi về đi, đứng đây nữa trời tối mất.",
                        "Bảo: Nhà bà họ hàng của Nam cho thuê rẻ thế này là may rồi. Ở một thời gian cũng quen.",
                        "Nam: Bà nhờ mình trông nhà hộ. Có phòng thờ riêng, bà dặn đừng bày đồ chắn lối.",
                        "Băng: Về còn ăn cơm. Hôm nay đến lượt ai rửa bát ấy nhỉ?" } }
            }).ToArray();
            var street = Room(FirstRoom, "Đầu ngõ", "PROLOGUE · HÀ NỘI, NĂM 2004",
                "Tôi là Nam, sinh viên năm nhất Đại học Thủy Lợi.\n\nTôi và ba người bạn đã ở căn nhà này được một thời gian. Chiều hôm đó, chúng tôi trở về như mọi ngày.",
                new Color(.29f,.28f,.25f),
                new AdventureHotspot { id = "PROLOGUE_DOG", label = "Chó đầu ngõ\n[NHÂN VẬT TẠM]", kind = InteractionKind.ReadClue,
                    bounds = new Rect(.06f,.3f,.23f,.29f), visualClass = "friend", clueId = "PROLOGUE_DOG_CLUE",
                    solvesPuzzle = "PROLOGUE_DOG_SEEN", response = "[Chó sủa] Cả nhóm đi tiếp vào ngách." },
                Next("PROLOGUE_STREET_NEXT", "Theo các bạn vào ngách →", "PROLOGUE_ALLEY", "PROLOGUE_DOG_SEEN"));
            var alley = Room("PROLOGUE_ALLEY", "Ngách cuối khu tập thể", "PROLOGUE · ĐƯỜNG VỀ NHÀ",
                "Tiếng xe ngoài phố nhỏ dần sau lưng.\n\nTrước mặt là ngách dẫn tới căn nhà nhóm đang thuê. Bia, Băng và Bảo vừa đi vừa nói chuyện.",
                new Color(.25f,.27f,.26f),
                new AdventureHotspot { id = "PROLOGUE_GROUP", label = "Bia · Băng · Bảo\n[NHÂN VẬT TẠM]\nNói chuyện", kind = InteractionKind.Talk,
                    bounds = new Rect(.06f,.26f,.23f,.37f), visualClass = "friend", dialogueId = "PROLOGUE_GROUP_DIALOGUE",
                    solvesPuzzle = "PROLOGUE_GROUP_TALKED", response = "Cả nhóm tiếp tục đi về phía căn nhà." },
                Next("PROLOGUE_ALLEY_NEXT", "Đi tới cửa nhà →", "PROLOGUE_ENTRY", "PROLOGUE_GROUP_TALKED"));
            var entry = Room("PROLOGUE_ENTRY", "Trước cửa nhà trọ", "PROLOGUE · CĂN NHÀ QUEN THUỘC",
                "[Tiếng quạt điện vọng từ trong nhà]\n\nCánh cửa quen thuộc ở cuối ngách. Nam đưa tay đẩy cửa. Một buổi tối bình thường nữa, cậu nghĩ vậy.",
                new Color(.24f,.21f,.18f),
                new AdventureHotspot { id = "PROLOGUE_DOOR", label = "Cửa nhà\n[HÌNH TẠM]\nMở cửa", kind = InteractionKind.Inspect,
                    bounds = new Rect(.06f,.23f,.23f,.4f), visualClass = "door", solvesPuzzle = "PROLOGUE_DOOR_SEEN",
                    response = "Cửa mở. Tiếng quạt và tiếng trò chuyện trong nhà nghe rõ hơn." },
                Next("PROLOGUE_ENTRY_NEXT", "Bước vào nhà →", "PROLOGUE_TITLE", "PROLOGUE_DOOR_SEEN"));
            var title = Room("PROLOGUE_TITLE", "Fabricated Dawn", "PROLOGUE · KẾT THÚC PHẦN MỞ ĐẦU",
                "FABRICATED\nDAWN", new Color(.055f,.045f,.04f),
                Next("PROLOGUE_BEGIN_CHAPTER_1", "Màn 1 — Những ngày ở trọ →", "ROOM_01", ""));
            title.isTitleCard = true;
            title.hotspots[0].solvesPuzzle = "PROLOGUE_DONE";
            content.rooms = new[] { street, alley, entry, title }.Concat(content.rooms).ToArray();

            var milestone = content.Room("DEMO_NORMAL");
            if (milestone != null)
            {
                milestone.id = "DEMO_M2_SHRINE";
                milestone.title = "Phòng thờ · Qua đêm đầu tiên";
                milestone.subtitle = "MÀN 2 HOÀN TẤT · TIẾP THEO LÀ MÀN 3";
                milestone.ending = StoryEnding.None;
                milestone.chapter = 2;
                milestone.description = "";
                milestone.narration = "Nam chạy qua ngưỡng phòng thờ. Ba người bạn theo vào. Người phụ nữ dừng ngoài cửa.\n\nSáng mai, cả nhóm sẽ tìm hiểu chuyện nhà chủ cũ.\n\nMàn 3–8 đang được phát triển. Kết thúc thường sẽ nằm ở Màn 8.";
                milestone.hotspots = Array.Empty<AdventureHotspot>();
                var escape = content.rooms.SelectMany(r => r.hotspots).First(h => h.id == "DEMO_ESCAPE_SHRINE");
                escape.destinationRoom = milestone.id;
                escape.solvesPuzzle = "DEMO_M2_COMPLETE";
                escape.checkpointAfter = true;
            }
            content.initiallyUnlockedRooms = content.rooms.Select(r => r.id).ToArray();
        }

        private static AdventureObjective Objective(string id, string text) => new AdventureObjective { afterPuzzle = id, text = text };
        private static AdventureRoom Room(string id, string title, string subtitle, string narration, Color color, params AdventureHotspot[] hotspots) =>
            new AdventureRoom { id = id, title = title, subtitle = subtitle, narration = narration, placeholderColor = color, hotspots = hotspots };
        private static AdventureHotspot Next(string id, string label, string destination, string required) =>
            new AdventureHotspot { id = id, label = label, destinationRoom = destination, requiredPuzzle = required,
                kind = InteractionKind.Travel, bounds = new Rect(.6f,.69f,.34f,.12f), visualClass = "door", checkpointAfter = true,
                response = "Tiếp tục câu chuyện.", lockedResponse = "Hãy quan sát hoặc nói chuyện với nhóm trước khi đi tiếp." };

        internal static void PlayToChapterOne(AdventureSession session)
        {
            Act(session,"PROLOGUE_DOG"); Act(session,"PROLOGUE_STREET_NEXT");
            Act(session,"PROLOGUE_GROUP");
            while (session.ActiveDialogue != null) session.AdvanceDialogue();
            Act(session,"PROLOGUE_ALLEY_NEXT"); Act(session,"PROLOGUE_DOOR");
            Act(session,"PROLOGUE_ENTRY_NEXT"); Act(session,"PROLOGUE_BEGIN_CHAPTER_1");
        }
        private static void Act(AdventureSession session, string id)
        {
            if (!session.Interact(id).succeeded) throw new InvalidOperationException("Prologue interaction failed: " + id);
        }
    }
}
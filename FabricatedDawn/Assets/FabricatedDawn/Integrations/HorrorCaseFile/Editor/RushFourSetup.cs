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
    // GDD 5.2–5.5. Dialogue wording and puzzle choices are implementation drafts.
    public static class RushFourSetup
    {
        public const string Data="Assets/FabricatedDawn/Data/DemoFour";
        public const string ContentPath=Data+"/RushDemo.asset";
        public const string ScenePath="Assets/FabricatedDawn/Scenes/RushDemo.unity";
        public const string ArtRoot="Assets/FabricatedDawn/Art/RushFour";
        static AdventureDefinition content;
        static List<AdventureRoom> rooms;
        static List<ClueDefinition> clues;
        static List<DialogueDefinition> dialogues;
        static List<ItemDefinition> items;
        static ItemDefinition sideKey,ward;

        [MenuItem("Tools/Fabricated Dawn/Demo Màn 1–4/Chuẩn bị và mở")]
        public static void Open()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Dừng Play trước khi chuẩn bị.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Data);AssetDatabase.Refresh();ImportArt();
            var source=AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ChapterOneSetup.ContentPath);
            if(source==null)throw new InvalidOperationException("Thiếu dữ liệu Màn 1.");
            content=AssetDatabase.LoadAssetAtPath<AdventureDefinition>(ContentPath);
            if(content==null){content=ScriptableObject.CreateInstance<AdventureDefinition>();AssetDatabase.CreateAsset(content,ContentPath);}
            EditorUtility.CopySerialized(source,content);
            content.name="RushDemo";content.contentId="fabricated-dawn-rush-four-v1";
            content.startRoomId="RUSH_INTRO";content.completionPuzzleId="M4_COMPLETE";
            content.completedObjective="Màn 4 hoàn tất. Cả nhóm bị giữ lại trong căn nhà cũ; câu chuyện vẫn còn tiếp.";
            rooms=content.rooms.ToList();clues=content.clues.ToList();dialogues=content.dialogues.ToList();items=content.items.ToList();
            foreach(var room in rooms){
                room.presentOnly=true;
                var old=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(room.presentBackground));
                room.nightScene=old.EndsWith("-night",StringComparison.Ordinal)||room.id.StartsWith("DEMO_CHASE");
                string name=old.Replace("-night","");
                if(!string.IsNullOrEmpty(name)&&BG(name)!=null)room.presentBackground=BG(name);
                room.pastBackground=room.presentBackground;
                foreach(var h in room.hotspots)h.response=(h.response??"").Replace("[Phụ đề âm thanh] ","");
            }
            ApplyOpeningAndM1();ApplyM2();BuildM3();BuildM4();
            content.rooms=rooms.ToArray();content.clues=clues.ToArray();content.dialogues=dialogues.ToArray();content.items=items.ToArray();
            content.initiallyUnlockedRooms=rooms.Select(r=>r.id).ToArray();
            content.initialObjective="Hà Nội, 2004 · Những ngày ở trọ";
            content.objectiveUpdates=new[]{O("RUSH_INTRO_DONE","Dọn đồ: tìm chìa trên bàn, chọn chìa rồi mở tủ.")}
                .Concat(content.objectiveUpdates.Where(o=>o.afterPuzzle!="DEMO_M2_COMPLETE"))
                .Concat(new[]{
                    O("DEMO_M2_COMPLETE","Đã tới phòng thờ. Nói chuyện với bác quản lý, đợi trời sáng."),
                    O("M3_BEGIN","Tìm giấy thuê và ảnh gia đình; gọi về nhà, hỏi bà cho thuê và người hàng xóm."),
                    O("M3_EVIDENCE","Đối chiếu giấy thuê, ảnh, lời người nhà và lời hàng xóm ở bàn phòng khách."),
                    O("M3_COMPARE","Lấy chìa cửa phụ trong ngăn tủ. Ghé hàng tạp hóa, hỏi người biết việc thờ cúng rồi nhận vật trấn."),
                    O("M3_PACK","Đã gom hành lý. Thử xuống cầu thang để rời căn nhà."),
                    O("M4_BEGIN","Cầu thang đưa nhóm trở lại. Thử lối đi, ghi lại thứ không đổi qua mỗi lượt."),
                    O("M4_MARK_B","Đối chiếu ô gạch dưới cửa thoáng với sơ đồ sửa nhà. Chọn lối dẫn về phía bếp."),
                    O("M4_LOOP","Đã tìm được cửa phụ. Chọn chìa trong túi rồi mở chốt theo sơ đồ."),
                    O("M4_SIDE_DOOR","Cửa đã mở khóa. Bước qua cửa phụ để tìm lối ra."),
                    O("M4_CHANGED","Căn nhà đã đổi khác. Kiểm tra cửa ra, cửa sổ và căn phòng vừa xuất hiện."),
                    O("M4_WINDOW","Tìm căn phòng giống trong ảnh gia đình và đối chiếu đồ vật bên trong.")
                }).ToArray();
            EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
            if(!File.Exists(ScenePath)&&!AssetDatabase.CopyAsset(ChapterOneSetup.ScenePath,ScenePath))throw new IOException("Thiếu scene mẫu.");
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var ui=UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if(ui==null)throw new InvalidOperationException("Thiếu giao diện Case File.");
            ui.content=content;EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            AssetDatabase.SaveAssets();RushFourVerification.Verify();Debug.Log("RUSH_FOUR_READY: "+ScenePath);
        }

        static void ApplyOpeningAndM1()
        {
            var start=Go("RUSH_INTRO_NEXT","Bước vào những ngày ở trọ ›","M1_BEDROOM",.33f,.75f,.34f,.085f);
            start.solvesPuzzle="RUSH_INTRO_DONE";start.checkpointAfter=true;
            var intro=R("RUSH_INTRO","Hà Nội · năm 2004",1,"living",start);intro.nightScene=true;intro.introCard=true;
            intro.narration="HÀ NỘI, NĂM 2004\n\nTôi là Trần Văn Nam, sinh viên Đại học Thủy Lợi. Tôi cùng Bia, Băng và Bảo thuê chung một căn nhà để tiện đi học.\n\nNhà vốn của họ hàng bên nội. Bà cho thuê rẻ, nhờ chúng tôi trông nom và giữ yên tĩnh ở phòng thờ trên gác.\n\nỞ được một thời gian, những chuyện lạ bắt đầu.";
            D("M1_BIA_TALK","Bia",null,null,"Cậu xếp nốt sách vở đi. Lát xuống ăn cơm, đừng để mọi người đợi.","Bà cho thuê rẻ cũng là giúp bọn mình. Nhớ trông cửa nẻo cho cẩn thận.");
            D("M1_BANG_TALK","Băng",null,null,"Hôm nay tớ nấu, mai đến lượt cậu đấy. Bếp ở sau nhà, cạnh lối cầu thang.","Tớ lấy bốn cái bát rồi. Ăn xong thì phụ nhau rửa cho nhanh.");
            D("M1_BAO_TALK","Bảo",null,null,"Tớ vừa ghé hàng đầu ngõ. Thiếu gì thì lát ra mua, đừng để đến tối.","Nhà vẫn chia phòng như hồi chủ cũ ở. Phòng thờ trên gác là một phòng riêng, cậu nhớ nhé.");
            D("M1_END_TALK","Bia · Băng · Bảo · Nam",null,null,"Băng: Hôm trước ai bày thêm cái bát nhỏ thế? Tớ nhớ mình chỉ lấy bốn cái.","Nam: Tớ còn gặp một quả bóng đỏ ngoài hành lang. Lúc trở về, nó đã nằm trước cửa phòng.","Bảo: Chắc có đứa trêu thôi. Hay bốn đứa thử Vỗ Vai Ma một lượt cho biết?","Bia: Chơi một lát thôi đấy. Dọn phòng khách cho rộng đã.");
            foreach(var room in rooms.Where(r=>r.id=="M1_LIVING"||r.id=="M1_READY")){
                foreach(var h in room.hotspots.Where(h=>h.kind==InteractionKind.Talk)){
                    string tag=h.id.Contains("BANG")?"Băng":h.id.Contains("BAO")?"Bảo":"Bia";
                    int slot=tag=="Bia"?0:tag=="Băng"?1:2;Person(h,tag,.22f+slot*.20f);
                }
            }
            var mirror=H("M1_MIRROR");mirror.artwork=null;mirror.stickFigure=true;mirror.characterTag="?";mirror.artworkBounds=mirror.bounds;
            var note=clues.First(c=>c.id=="M1_NOTE_CLUE");note.body="Nam, xếp đồ xong thì xuống ăn nhé. Bia ở phòng khách, Băng lo bữa cơm, Bảo vừa ra hàng đầu ngõ.\n\nBà dặn trông nom căn nhà, đừng để đồ chắn cửa phòng thờ trên gác. — Bia";
            rooms.First(r=>r.id=="M1_LATER").narration="QUA VÀI NGÀY\n\nVẫn những buổi học, bữa cơm và chuyện vặt.\nNhững điều lạ trong nhà chưa có lời giải.";
        }

        static void ApplyM2()
        {
            Room("DEMO_BAD").presentBackground=BG("hall");Room("DEMO_BAD").pastBackground=BG("hall");Room("DEMO_BAD").nightScene=true;
            D("DEMO_GAME_DIALOGUE","Bia",null,null,"Mỗi đứa đứng một góc. Tắt đèn rồi người đầu đi dọc tường, vỗ vai người kế bên.","Ai được vỗ vai thì đi tiếp. Tới góc trống phải dừng lại. Chỉ có bốn đứa mình thôi.");
            var game=Room("DEMO_LIVING");game.nightScene=true;
            for(int n=1;n<=4;n++){
                var corner=game.hotspots.First(h=>h.id=="DEMO_CORNER_HOTSPOT_"+n);
                corner.sceneHotspot=true;corner.artwork=null;corner.stickFigure=true;corner.characterTag=n==4?"?":new[]{"Bia","Băng","Bảo"}[n-1];
                corner.bounds=new Rect(.08f+(n-1)*.22f,.32f,.17f,.35f);corner.artworkBounds=corner.bounds;
                corner.label=n==4?"Góc đáng lẽ đã trống":"Đi tới góc của "+corner.characterTag;
            }
            H("DEMO_CORNER_HOTSPOT_4").response="Một bờ vai lạnh ngắt. Nam bật đèn: vẫn chỉ có bốn người. Cánh cửa ngoài phòng khách chầm chậm mở.";
            var empty=H("DEMO_CORNER_HOTSPOT_4");empty.stickFigure=false;empty.artworkBounds=default;empty.label="Góc trống · đưa tay về phía trước";
            var stranger=Inspect("RUSH_STRANGER","Bóng người ngoài ngưỡng cửa",.79f,.28f,.14f,.40f,"Dáng người đứng im ngoài cửa. Không ai trong nhóm biết bà ta là ai.");
            Person(stranger,"?",.77f);stranger.visibleAfterPuzzle="DEMO_CORNER_4";game.hotspots=game.hotspots.Concat(new[]{stranger}).ToArray();
            foreach(var room in rooms.Where(r=>r.id.StartsWith("DEMO_CHASE")))foreach(var h in room.hotspots){
                if(h.destinationRoom=="DEMO_BAD")h.protectionItem=content.Item("M1_AMULET");
            }
            var safe=Room("DEMO_M2_SHRINE");safe.title="Phòng thờ · qua cơn hoảng sợ";safe.nightScene=true;
            safe.narration="Bóng người đàn bà dừng ngoài ngưỡng cửa.\nMột lát sau, đèn trong nhà sáng trở lại.";
            D("RUSH_MANAGER_TALK","Bác quản lý",null,null,"Bác nghe các cháu gọi nên lên xem. Dây điện nhà này chập chờn, bác vừa nối lại rồi.","Đêm hôm đừng tụ tập làm ầm nữa. Có việc gì thì sáng mai hãy hỏi.");
            var manager=Talk("RUSH_MANAGER","Hỏi bác quản lý","RUSH_MANAGER_TALK",.20f);Person(manager,"Bác quản lý",.24f);
            var morning=Go("RUSH_TO_M3","Đợi trời sáng ›","M3_HOME",.77f,.62f,.20f,.10f);
            morning.requiredEvent="dialogue:RUSH_MANAGER_TALK";morning.solvesPuzzle="M3_BEGIN";morning.checkpointAfter=true;
            safe.hotspots=new[]{manager,morning};
        }

        static void BuildM3()
        {
            sideKey=Item("RUSH_SIDEKEY","Chìa cửa phụ","Chìa có khắc một vạch nhỏ. Giấy sửa nhà đánh dấu đúng chốt cửa phía sau.","fd-022");
            ward=Item("RUSH_WARD","Vật trấn nhỏ","Vật người biết việc thờ cúng gửi nhóm mang theo. Chưa biết có giúp được gì.","fd-021");
            var incense=Item("RUSH_INCENSE","Bó nhang","Mua ở hàng đầu ngõ để dùng khi cần thắp hương.","fd-019");
            var matches=Item("RUSH_MATCHES","Bao diêm","Một bao diêm giấy bình thường.","fd-018");
            var batteries=Item("RUSH_BATTERIES","Đôi pin","Pin mua để phòng khi mất điện.","fd-017");
            C("M3_LEASE","Giấy thuê nhà","Giấy thuê có tên người bà đứng ra cho thuê. Bà nói căn nhà lâu nay chỉ để trống; trên giấy vẫn còn tên một người đàn ông cùng họ bên nội của Nam.");
            C("M3_PHOTO","Ảnh gia đình chủ cũ","Trong ảnh có một người đàn ông, vợ và con gái. Họ đứng trước tủ gỗ, cạnh thỏ vải và quả bóng đỏ. Sau lưng họ còn một cửa phòng nhỏ, nay không thấy trên hành lang.");
            C("M3_FAMILY","Lời người nhà","Người đàn ông trong ảnh thuộc họ bên nội của Nam. Ký hiệu trên vật hộ thân là dấu dòng họ. Đây không phải việc chỉ tình cờ trùng tên.");
            C("M3_GRANDMA","Lời bà cho thuê","Bà nhận con trai đã mất, nhưng né chuyện con dâu. Lời bà không khớp với việc bà từng bảo nhà không có chuyện gì đáng ngại.");
            C("M3_NEIGHBOR","Lời người hàng xóm","Hàng xóm kể bé gái mất trước; về sau người vợ giết chồng rồi tự sát. Đây mới là lời kể cần đối chiếu, chưa giải thích nguyên nhân hay toàn bộ chuyện trong nhà.");
            C("M3_RENOVATION","Sơ đồ sửa nhà","Cầu thang ở bên phải, bếp và cửa phụ phía sau. Dưới cửa thoáng ở chiếu nghỉ có một ô gạch bông khuyết cánh. Ô gạch này chưa thay dù các phòng từng sửa lại. Một phòng nhỏ cạnh phòng ngủ sau nhà đã bị bịt cửa. Chốt cửa phụ ứng với dấu một vạch trên chìa.");
            D("M3_FAMILY_TALK","Người nhà · qua điện thoại","M3_FAMILY",null,"Người trong ảnh là người bên họ nội nhà mình. Dấu trên vật con đeo cũng là dấu dòng họ ấy.","Con hỏi cho rõ rồi hãy tính. Nếu thấy không yên thì dọn đi, đừng vì tiếc chỗ trọ rẻ mà cố ở.");
            D("M3_GRANDMA_TALK","Bà cho thuê · qua điện thoại","M3_GRANDMA",null,"Con trai bà mất đã lâu rồi. Còn chuyện con dâu, bà không muốn nhắc nữa.","Các cháu cứ lo việc học đi. Nhà cửa có gì thì báo bà.");
            D("M3_NEIGHBOR_TALK","Người hàng xóm","M3_NEIGHBOR",null,"Bác ở đây từ trước. Nhà ấy có đứa bé gái mất, rồi lại xảy ra chuyện vợ chồng nó.","Người vợ giết chồng rồi tự vẫn. Bác chỉ biết chuyện xảy ra như thế, còn đầu đuôi thì không dám nói bừa.");
            D("M3_ELDER_TALK","Người biết việc thờ cúng","M3_RENOVATION","M3_ELDER","Nhà đã sửa lối đi, nhưng ô gạch dưới cửa thoáng ở chiếu nghỉ vẫn còn. Lúc bối rối, nhìn thứ quen thuộc ấy mà định đường.","Cửa phụ ở phía bếp có chốt một vạch. Các cháu cầm vật này, giữ bên mình. Ta chỉ biết chừng ấy, không dám nhận đã hiểu hết chuyện nhà người ta.");
            D("M3_PACK_TALK","Cả nhóm",null,"M3_PACK","Nam: Giấy tờ và lời kể đều chỉ về căn nhà này. Bà đã giấu chuyện, còn gia đình cũ lại có quan hệ với nhà tớ.","Bia: Vậy dọn đi. Cứ thu xếp đồ, xuống dưới rồi tính chỗ khác.","Băng: Sách vở với quần áo gói xong rồi. Đi cùng nhau, đừng để ai ở lại một mình.");
            var house=R("M3_HOME","Phòng khách · sáng hôm sau",3,"living",
                Read("M3_READ_LEASE","Xem giấy thuê nhà","M3_LEASE","",.19f,.61f,.13f,.14f,Art("fd-010")),
                Read("M3_READ_PHOTO","Xem ảnh gia đình","M3_PHOTO","",.42f,.50f,.12f,.17f,Art("fd-005")),
                Go("M3_HOME_PHONE","Gọi điện về nhà ›","M3_PHONE",.02f,.28f,.18f,.11f),
                Go("M3_HOME_STREET","Ra đầu ngõ ›","M3_STREET",.02f,.67f,.18f,.09f),
                Go("M3_HOME_BOARD","Đối chiếu lời kể ›","M3_BOARD",.44f,.65f,.22f,.10f),
                Go("M3_HOME_PACK","Lên gác gom hành lý ›","M3_PACK_ROOM",.77f,.43f,.20f,.12f));
            var phone=R("M3_PHONE","Điện thoại bàn · hỏi chuyện nhà",3,"bedroom",
                Talk("M3_CALL_FAMILY","Gọi người nhà","M3_FAMILY_TALK",.12f),
                Talk("M3_CALL_GRANDMA","Gọi bà cho thuê","M3_GRANDMA_TALK",.38f),
                Go("M3_PHONE_BACK","Xuống phòng khách ›","M3_HOME",.78f,.56f,.19f,.10f));
            var neighbor=Talk("M3_ASK_NEIGHBOR","Hỏi người hàng xóm","M3_NEIGHBOR_TALK",.14f);Person(neighbor,"Hàng xóm",.20f);
            R("M3_STREET","Đầu ngõ · hỏi người từng biết nhà",3,"alley",neighbor,
                Go("M3_STREET_SHOP","Ghé hàng tạp hóa ›","M3_SHOP",.31f,.30f,.18f,.13f),
                Go("M3_STREET_HOME","Trở về nhà ›","M3_HOME",.82f,.55f,.16f,.10f));
            var elder=Talk("M3_ASK_ELDER","Hỏi chuyện lối đi cũ","M3_ELDER_TALK",.14f);Person(elder,"Người lớn tuổi",.18f);
            var receive=Pick("M3_TAKE_WARD","Nhận vật trấn",ward,.40f,.57f);receive.visibleAfterPuzzle="M3_ELDER";
            R("M3_SHOP","Hàng đầu ngõ · mua ít đồ mang theo",3,"alley",elder,receive,
                Pick("M3_BUY_INCENSE","Mua bó nhang",incense,.57f,.56f),Pick("M3_BUY_MATCHES","Mua diêm",matches,.70f,.56f),Pick("M3_BUY_BATTERIES","Mua pin",batteries,.82f,.56f),
                Go("M3_SHOP_BACK","Về đầu ngõ ›","M3_STREET",.81f,.31f,.17f,.10f));
            var evidence=new[]{"M3_LEASE","M3_PHOTO","M3_FAMILY","M3_GRANDMA","M3_NEIGHBOR"};
            var crime=Inspect("M3_FACT_CRIME","Căn nhà từng có án mạng",.08f,.34f,.38f,.09f,"Lời hàng xóm khớp với những người trong ảnh.");crime.requiredClues=evidence;crime.solvesPuzzle="M3_FACT_CRIME";
            var hidden=Inspect("M3_FACT_HIDDEN","Bà cho thuê đã giấu chuyện",.52f,.34f,.38f,.09f,"Bà biết gia đình cũ, nhưng đã không nói chuyện xảy ra tại nhà.");hidden.requiredClues=evidence;hidden.solvesPuzzle="M3_FACT_HIDDEN";
            var kin=Inspect("M3_FACT_KIN","Nam có quan hệ phía người chồng",.08f,.47f,.38f,.09f,"Tên trên giấy, ảnh và dấu dòng họ cùng khớp với lời người nhà.");kin.requiredClues=evidence;kin.solvesPuzzle="M3_FACT_KIN";
            var combine=Inspect("M3_COMPARE","Chốt những điều đã có chứng cứ",.52f,.60f,.38f,.10f,"Ba điều đã khớp. Chưa thể kết luận vì sao bi kịch xảy ra.");combine.requiredPuzzles=new[]{"M3_FACT_CRIME","M3_FACT_HIDDEN","M3_FACT_KIN"};combine.solvesPuzzle="M3_COMPARE";combine.checkpointAfter=true;
            R("M3_BOARD","Đối chiếu · những thông tin không khớp",3,"living",crime,hidden,kin,combine,
                Inspect("M3_WRONG_FACT","Đã biết nguyên nhân cái chết của bé?",.52f,.47f,.38f,.09f,"Chưa đủ chứng cứ. Lời kể ngoài ngõ chưa nói rõ điều ấy."),
                Go("M3_BOARD_BACK","Rời bàn đối chiếu ›","M3_HOME",.03f,.68f,.25f,.09f));
            var takeKey=Pick("M3_TAKE_SIDEKEY","Lấy chìa cửa phụ",sideKey,.47f,.47f);takeKey.visibleAfterPuzzle="M3_COMPARE";
            var pack=Talk("M3_PACK_GROUP","Gom đồ, cùng nhau chuyển trọ","M3_PACK_TALK",.10f);
            pack.requiredPuzzles=new[]{"M3_COMPARE"};pack.requiredClues=new[]{"M3_RENOVATION"};pack.requiredEvents=new[]{"pickup:RUSH_SIDEKEY","pickup:RUSH_WARD"};
            pack.lockedResponse="Chưa thu xếp đủ. Hãy đối chiếu lời kể, lấy chìa cửa phụ và nhận vật trấn ở hàng đầu ngõ.";
            var leave=Go("M3_LEAVE","Xuống cầu thang rời nhà ›","M4_LOOP_A",.77f,.58f,.20f,.11f);leave.requiredEvent="dialogue:M3_PACK_TALK";leave.solvesPuzzle="M4_BEGIN";leave.checkpointAfter=true;
            R("M3_PACK_ROOM","Phòng ngủ · thu xếp hành lý",3,"bedroom",takeKey,pack,leave,
                Go("M3_PACK_BACK","Quay xuống phòng khách ›","M3_HOME",.03f,.68f,.25f,.09f));
            // One explicit evidence milestone helps the objective without guessing what the player knows.
            house.hotspots.First(h=>h.id=="M3_HOME_BOARD").solvesPuzzle="M3_EVIDENCE";
        }

        static void BuildM4()
        {
            C("M4_FIXED_A","Dấu mốc ở chiếu nghỉ","Lượt thứ nhất: dưới cửa thoáng vẫn là ô gạch bông khuyết cánh. Biển phòng và hướng cầu thang trông không giống lúc sáng.");
            C("M4_FIXED_B","Dấu mốc vẫn còn nguyên","Lượt kế tiếp: biển phòng đã đổi, nhưng ô gạch khuyết cánh ở nguyên chỗ. Sơ đồ sửa nhà chỉ đường từ đây về phía bếp và cửa phụ.");
            C("M4_EXIT_CLUE","Cửa sau không dẫn ra ngõ","Cánh cửa vừa mở đã đưa cả nhóm trở về lối vào. Phía sau chỉ còn một bức tường kín. Căn nhà và đồ đạc đều đã đổi cùng một lúc.");
            C("M4_WINDOW_CLUE","Bên ngoài cửa sổ","Ngoài cửa sổ không còn là ngõ quen thuộc. Những khoảng tối không nối được với đường phố. Không thể bước ra theo lối này.");
            C("M4_OBJECT_CLUE","Căn phòng trong ảnh","Cửa phòng từng bị sửa mất nay đã xuất hiện. Tủ gỗ, thỏ vải và chỗ đặt đồ đều giống ảnh gia đình tìm được lúc sáng.");
            foreach(var name in new[]{"A","B"}){
                var mark=Read("M4_MARK_"+name,"Nhìn ô gạch dưới cửa thoáng",name=="A"?"M4_FIXED_A":"M4_FIXED_B",name=="A"?"M4_MARK_A":"M4_MARK_B",.51f,.65f,.12f,.10f,null);
                var down=Go("M4_STAIRS_"+name,"Thử xuống cầu thang",name=="A"?"M4_LOOP_B":"M4_LOOP_A",.78f,.50f,.18f,.11f);
                var door=Go("M4_WRONG_"+name,"Phòng "+(name=="A"?"203":"302")+" · thử đi lối này",name=="A"?"M4_LOOP_B":"M4_LOOP_A",.26f,.26f,.17f,.13f);
                down.response=door.response="Đi hết lối, cả nhóm lại đứng trước chiếu nghỉ cũ. Một tiếng ho khan; bóng đàn ông vừa khuất ở cuối dãy. Nam không kịp nhìn mặt.";
                var correct=Go("M4_ROUTE_"+name,"Theo dấu gạch, tìm lối về bếp ›","M4_SIDE",.03f,.49f,.31f,.11f);
                correct.requiredPuzzles=new[]{"M4_MARK_A","M4_MARK_B"};correct.requiredClues=new[]{"M3_RENOVATION","M1_ROUTE_CLUE"};correct.solvesPuzzle="M4_LOOP";correct.checkpointAfter=true;
                correct.lockedResponse="Đừng vội theo biển phòng. Cần quan sát dấu mốc ở hai lượt và đối chiếu sơ đồ sửa nhà.";
                var room=R("M4_LOOP_"+name,"Chiếu nghỉ · phòng "+(name=="A"?"203":"302"),4,"hall",mark,down,door,correct);
                room.nightScene=true;
            }
            var unlock=Inspect("M4_UNLOCK","Dùng chìa mở chốt một vạch",.51f,.37f,.16f,.22f,"Chốt vừa khít dấu trên chìa. Cửa phụ đã mở khóa.");
            unlock.kind=InteractionKind.UseItem;unlock.item=sideKey;unlock.requiredPuzzle="M4_LOOP";unlock.requiredClues=new[]{"M3_RENOVATION"};unlock.solvesPuzzle="M4_SIDE_DOOR";unlock.checkpointAfter=true;unlock.hiddenAfterPuzzle="M4_SIDE_DOOR";
            var open=Go("M4_OPEN_DOOR","Mở cửa phụ, bước ra ngoài ›","M4_PAST_ENTRY",.49f,.25f,.22f,.39f);
            open.requiredPuzzle="M4_SIDE_DOOR";open.visibleAfterPuzzle="M4_SIDE_DOOR";open.opensPastDoor=true;open.solvesPuzzle="M4_CHANGED";open.checkpointAfter=true;
            open.response="Cửa mở. Chỉ một bước chân, cả nhóm đã trở lại lối vào — nhưng toàn bộ căn nhà là một hình dạng khác.";
            var side=R("M4_SIDE","Cửa phụ sau bếp",4,"kitchen",unlock,open,Go("M4_SIDE_BACK","Quay về chiếu nghỉ ›","M4_LOOP_A",.02f,.53f,.20f,.10f));side.nightScene=true;
            var past=R("M4_PAST_ENTRY","Lối vào · căn nhà năm xưa",4,"living-past",
                Read("M4_CHECK_EXIT","Kiểm tra cánh cửa vừa đi qua","M4_EXIT_CLUE","M4_EXIT",.02f,.53f,.20f,.13f,null),
                Read("M4_CHECK_WINDOW","Nhìn ra cửa sổ","M4_WINDOW_CLUE","M4_WINDOW",.06f,.26f,.15f,.23f,null),
                Go("M4_PAST_UP","Lên chiếu nghỉ ›","M4_PAST_HALL",.78f,.38f,.18f,.18f));MakePast(past);
            var hall=R("M4_PAST_HALL","Hành lang cũ · một cửa phòng vừa hiện ra",4,"hall-past",
                Go("M4_TO_HIDDEN","Cửa phòng trong ảnh ›","M4_HIDDEN",.145f,.25f,.09f,.44f),
                Go("M4_PAST_DOWN","Xuống lối vào ›","M4_PAST_ENTRY",.78f,.56f,.18f,.13f));MakePast(hall);
            var child=R("M4_HIDDEN","Căn phòng bị sửa mất",4,"hidden-room",
                Read("M4_MATCH_OBJECT","Đối chiếu thỏ vải với ảnh","M4_OBJECT_CLUE","M4_OBJECT",.53f,.52f,.14f,.18f,Art("fd-003")),
                Inspect("M4_CONFIRM","Căn nhà đã khép kín quanh chúng ta",.12f,.31f,.36f,.13f,"Cửa ra không còn, bên ngoài đã đổi, căn phòng trong ảnh lại hiện ra. Không phải chỉ đi lạc. Cả nhóm đang bị giữ trong một phiên bản khác của căn nhà."),
                Go("M4_HIDDEN_BACK","Ra hành lang cũ ›","M4_PAST_HALL",.84f,.48f,.14f,.18f));MakePast(child);
            var confirm=child.hotspots.First(h=>h.id=="M4_CONFIRM");confirm.requiredPuzzles=new[]{"M4_EXIT","M4_WINDOW","M4_OBJECT"};confirm.solvesPuzzle="M4_COMPLETE";confirm.checkpointAfter=true;
            var finish=Go("M4_REST","Cả nhóm ngồi lại tính đường ›","M4_MILESTONE",.30f,.68f,.36f,.09f);finish.visibleAfterPuzzle="M4_COMPLETE";child.hotspots=child.hotspots.Concat(new[]{finish}).ToArray();
            var milestone=R("M4_MILESTONE","Cuối Màn 4 · phía sau cánh cửa",4,"living-past");MakePast(milestone);
            milestone.narration="CĂN NHÀ KHÔNG CÒN LỐI RA\n\nBốn người hiểu rằng muốn thoát, họ phải lần lại chuyện cũ ngay trong những căn phòng này.\n\nMàn 4 hoàn tất · câu chuyện còn tiếp.";
        }

        static AdventureRoom R(string id,string title,int chapter,string bg,params AdventureHotspot[] hs){var r=new AdventureRoom{id=id,title=title,chapter=chapter,subtitle="MÀN "+chapter+" · "+(chapter==3?"CHUYỆN NHÀ CHỦ CŨ":chapter==4?"CÁNH CỬA TRỞ VỀ":"NHỮNG NGÀY Ở TRỌ"),presentBackground=BG(bg),pastBackground=BG(bg),presentOnly=true,hotspots=hs};rooms.Add(r);return r;}
        static void MakePast(AdventureRoom r){r.pastOnly=true;r.presentOnly=false;}
        static AdventureRoom Room(string id)=>rooms.First(r=>r.id==id);
        static AdventureHotspot H(string id)=>rooms.SelectMany(r=>r.hotspots).First(h=>h.id==id);
        static AdventureHotspot Inspect(string id,string label,float x,float y,float w,float h,string response)=>new AdventureHotspot{id=id,label=label,kind=InteractionKind.Inspect,bounds=new Rect(x,y,w,h),response=response};
        static AdventureHotspot Go(string id,string label,string dest,float x,float y,float w,float h){var a=Inspect(id,label,x,y,w,h,"Đã tới lối tiếp theo.");a.kind=InteractionKind.Travel;a.destinationRoom=dest;a.sceneHotspot=true;return a;}
        static AdventureHotspot Read(string id,string label,string clue,string puzzle,float x,float y,float w,float h,Sprite art){var a=Inspect(id,label,x,y,w,h,"Nam ghi lại điều vừa thấy vào sổ.");a.kind=InteractionKind.ReadClue;a.clueId=clue;a.solvesPuzzle=puzzle;a.artwork=art;a.sceneHotspot=true;return a;}
        static AdventureHotspot Talk(string id,string label,string dialogue,float x){var a=Inspect(id,label,x,.42f,.24f,.12f,"Cuộc chuyện trò tiếp tục.");a.kind=InteractionKind.Talk;a.dialogueId=dialogue;return a;}
        static AdventureHotspot Pick(string id,string label,ItemDefinition item,float x,float y){var a=Inspect(id,label,x,y,.11f,.15f,"Đã cất "+item.displayName.ToLowerInvariant()+" vào túi.");a.kind=InteractionKind.Pickup;a.item=item;a.sceneHotspot=true;return a;}
        static void Person(AdventureHotspot h,string name,float x){h.artwork=null;h.stickFigure=true;h.characterTag=name;h.artworkBounds=new Rect(x,.26f,.19f,.47f);h.bounds=new Rect(x+.025f,.27f,.14f,.44f);h.sceneHotspot=true;}
        static void C(string id,string title,string body)=>clues.Add(new ClueDefinition{id=id,title=title,body=body});
        static void D(string id,string speaker,string clue,string puzzle,params string[] lines){var d=dialogues.FirstOrDefault(x=>x.id==id);if(d==null){d=new DialogueDefinition{id=id};dialogues.Add(d);}d.speaker=speaker;d.clueOnComplete=clue;d.puzzleOnComplete=puzzle;d.lines=lines;}
        static AdventureObjective O(string id,string text)=>new AdventureObjective{afterPuzzle=id,text=text};
        static Sprite BG(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot+"/"+name+".png");
        static Sprite Art(string id){var path=Directory.GetFiles("Assets/FabricatedDawn/Art/AssetVault","*.png",SearchOption.AllDirectories).FirstOrDefault(p=>new DirectoryInfo(Path.GetDirectoryName(p)).Name.StartsWith(id+"_",StringComparison.Ordinal));return path==null?null:AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\','/'));}
        static ItemDefinition Item(string id,string title,string purpose,string art){var path=Data+"/"+id+".asset";var i=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);if(i==null){i=ScriptableObject.CreateInstance<ItemDefinition>();AssetDatabase.CreateAsset(i,path);}i.id=id;i.displayName=title;i.purpose=purpose;i.icon=Art(art);i.uses=1;EditorUtility.SetDirty(i);items.Add(i);return i;}
        static void ImportArt(){if(!Directory.Exists(ArtRoot))return;foreach(var file in Directory.GetFiles(ArtRoot,"*.png")){var t=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(t==null)continue;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.maxTextureSize=4096;t.SaveAndReimport();}}
    }
}

using System;
using System.IO;
using System.Linq;
using FabricatedDawn.Editor;
using UnityEditor;
using UnityEngine;

namespace FabricatedDawn.CaseFile.Editor
{
    public static class RushFourVerification
    {
        [MenuItem("Tools/Fabricated Dawn/Demo Màn 1–4/Kiểm tra mạch chơi")]
        public static void Verify()
        {
            var c=AssetDatabase.LoadAssetAtPath<AdventureDefinition>(RushFourSetup.ContentPath);
            var report=ToolkitVerification.Run(AssetDatabase.LoadAssetAtPath<AdventureDefinition>("Assets/FabricatedDawn/Data/Sandbox/ToolkitAdventure.asset"));
            Check(report,"GDD scope: opening, separate shrine, chapters1–4, eight planned, no early final ending",()=>{
                var errors=AdventureValidator.Errors(c);Require(errors.Count==0,string.Join("; ",errors));
                Require(c.startRoomId=="RUSH_INTRO"&&c.plannedChapterCount==8);
                Require(c.Room("RUSH_INTRO").narration.Contains("Đại học Thủy Lợi"));
                Require(c.Room("M1_SHRINE")!=null&&c.Room("M1_HALL").hotspots.Any(h=>h.destinationRoom=="M1_SHRINE"));
                Require(c.rooms.All(r=>r.ending!=StoryEnding.Good&&r.ending!=StoryEnding.True));
                Require(c.rooms.Where(r=>r.chapter>=3).All(r=>r.chapter<=4));
            });
            Check(report,"All active backgrounds and item icons resolve; people use replaceable stick figures",()=>{
                Require(c.rooms.All(r=>r.presentBackground!=null),"Missing background");
                Require(c.rooms.All(r=>AssetDatabase.GetAssetPath(r.presentBackground).StartsWith(RushFourSetup.ArtRoot)),"Mixed legacy backgrounds");
                Require(c.items.All(i=>i.icon!=null),"Missing item icon");
                foreach(var h in c.rooms.SelectMany(r=>r.hotspots).Where(h=>h.stickFigure))Require(h.artwork==null&&h.artworkBounds.width>0);
                Require(c.Room("M3_STREET").hotspots.Any(h=>h.stickFigure));
            });
            Check(report,"Full opening and M1 route reaches the game without Prologue",()=>{
                var s=BeforeChase(c);Require(s.Story.State.chapter==2&&s.Story.State.ending==StoryEnding.None);
                Require(s.Story.State.clues.Contains("M1_ROUTE_CLUE"));
                foreach(var clue in new[]{"M1_BOWL_CLUE","M1_HAND_CLUE","M1_BALLOON_CLUE"})Require(s.Story.State.clues.Contains(clue));
            });
            Check(report,"M2 first wrong turn spends exactly one amulet charge; second produces Bad End",()=>{
                var s=BeforeChase(c);var room=s.CurrentRoom.id;
                Act(s,"DEMO_ESCAPE_DOWN");Require(s.CurrentRoom.id==room&&s.Story.State.ending==StoryEnding.None&&s.Story.State.protectionCharges==0);
                var load=new AdventureSession(c);load.Restore(s.Story.CaptureCheckpoint());
                Require(load.Story.State.protectionCharges==0);Act(load,"DEMO_ESCAPE_DOWN");Require(load.Story.State.ending==StoryEnding.Bad);
            });
            Check(report,"M2 bedroom rescue also allows returning to the separate shrine",()=>{
                var s=BeforeChase(c);Act(s,"DEMO_ESCAPE_UP");Act(s,"DEMO_ESCAPE_BED");Require(s.Story.State.ending==StoryEnding.None);
                Act(s,"DEMO_ESCAPE_SHRINE");Require(s.CurrentRoom.id=="DEMO_M2_SHRINE"&&!s.Completed);
                Require(!s.Interact("RUSH_TO_M3").succeeded);Talk(s,"RUSH_MANAGER");Act(s,"RUSH_TO_M3");Require(s.Story.State.chapter==3);
            });
            Check(report,"M3 conclusions require all evidence and never expose the child's hidden cause",()=>{
                var s=Morning(c);Act(s,"M3_HOME_BOARD");Require(!s.Interact("M3_FACT_CRIME").succeeded);
                Act(s,"M3_WRONG_FACT");Require(s.Story.State.ending==StoryEnding.None&&!s.Story.State.solvedPuzzles.Contains("M3_COMPARE"));
                Require(!s.Interact("M3_COMPARE").succeeded);
            });
            Check(report,"NPC clue appears only after final dialogue line; checkpoint preserves it",()=>{
                var s=Morning(c);Act(s,"M3_HOME_PHONE");Act(s,"M3_CALL_FAMILY");Require(!s.Story.State.clues.Contains("M3_FAMILY"));
                var load=new AdventureSession(c);load.Restore(s.Story.CaptureCheckpoint());Require(!load.Story.State.completedEvents.Contains("dialogue:M3_FAMILY_TALK"));
                Talk(load,"M3_CALL_FAMILY");Require(load.Story.State.clues.Contains("M3_FAMILY"));
                var again=new AdventureSession(c);again.Restore(load.Story.CaptureCheckpoint());Require(again.Story.State.clues.Contains("M3_FAMILY"));
            });
            Check(report,"Packing cannot skip comparison, side key and protective object",()=>{
                var s=Morning(c);Act(s,"M3_HOME_PACK");Require(!s.Interact("M3_PACK_GROUP").succeeded&&!s.Interact("M3_LEAVE").succeeded);
                Require(!s.Interact("M3_TAKE_SIDEKEY").succeeded);
            });
            Check(report,"M3 complete investigation, optional purchases, comparison and packing enter M4",()=>{
                var s=Loop(c);Require(s.Story.State.chapter==4&&s.CurrentRoom.id=="M4_LOOP_A");
                Require(s.Story.State.inventory.Exists(i=>i.id=="RUSH_WARD")&&s.Story.State.inventory.Exists(i=>i.id=="RUSH_SIDEKEY"));
                foreach(var id in new[]{"RUSH_INCENSE","RUSH_MATCHES","RUSH_BATTERIES"})Require(s.Story.State.inventory.Exists(i=>i.id==id));
            });
            Check(report,"M4 wrong paths retain evidence; map and two observations unlock stable route",()=>{
                var s=Loop(c);Require(!s.Interact("M4_ROUTE_A").succeeded);Act(s,"M4_MARK_A");
                var clues=s.Story.State.clues.Count;Act(s,"M4_WRONG_A");Require(s.CurrentRoom.id=="M4_LOOP_B"&&s.Story.State.clues.Count==clues);
                Require(!s.Interact("M4_ROUTE_B").succeeded);Act(s,"M4_MARK_B");
                var load=new AdventureSession(c);load.Restore(s.Story.CaptureCheckpoint());Act(load,"M4_ROUTE_B");Require(load.CurrentRoom.id=="M4_SIDE");
            });
            Check(report,"Side-door wrong item cannot consume anything or change the house",()=>{
                var s=Side(c);Require(!s.Interact("M4_OPEN_DOOR").succeeded);s.SelectItem("RUSH_WARD");Require(!s.Interact("M4_UNLOCK").succeeded);
                Require(s.Story.State.inventory.Exists(i=>i.id=="RUSH_WARD")&&s.Story.State.world==WorldPhase.Present);
                s.SelectItem("RUSH_SIDEKEY");Act(s,"M4_UNLOCK");Require(s.Story.State.world==WorldPhase.Present);
            });
            Check(report,"One door action changes whole world and checkpoints at old entrance; sanctuary intact",()=>{
                var s=Past(c);Require(s.CurrentRoom.id=="M4_PAST_ENTRY"&&s.Story.State.world==WorldPhase.PastEnclosed);
                Require(s.Story.State.completedEvents.Count(e=>e=="M4_DOOR_OPENED")==1);
                Require(s.Story.State.sanctuary==SanctuaryPhase.Active&&!s.Story.BreakSanctuary());
                Require(!s.Story.TryTravel("M3_STREET",true));
                var load=new AdventureSession(c);load.Restore(s.Story.CaptureCheckpoint());
                Require(load.CurrentRoom.id==s.CurrentRoom.id&&load.Story.State.world==WorldPhase.PastEnclosed&&load.Story.State.inventory.Count==s.Story.State.inventory.Count);
                var invalid=CheckpointCodec.Restore(s.Story.CaptureCheckpoint());invalid.roomId="M3_HOME";
                bool refused=false;try{load.Restore(CheckpointCodec.Capture(invalid));}catch(ArgumentException){refused=true;}Require(refused&&load.CurrentRoom.id=="M4_PAST_ENTRY");
            });
            Check(report,"M4 conclusion requires exits plus matching object, ends at milestone without final ending",()=>{
                var s=Past(c);Act(s,"M4_PAST_UP");Act(s,"M4_TO_HIDDEN");Require(!s.Interact("M4_CONFIRM").succeeded);
                Act(s,"M4_MATCH_OBJECT");Act(s,"M4_HIDDEN_BACK");Act(s,"M4_PAST_DOWN");Act(s,"M4_CHECK_EXIT");Act(s,"M4_CHECK_WINDOW");
                Act(s,"M4_PAST_UP");Act(s,"M4_TO_HIDDEN");var finish=Act(s,"M4_CONFIRM");Require(finish.checkpoint&&s.Completed);
                Act(s,"M4_REST");Require(s.CurrentRoom.id=="M4_MILESTONE"&&s.Story.State.ending==StoryEnding.None);
                Require(!s.Story.CanChooseEnding(StoryEnding.Good,false)&&!s.Story.CanChooseEnding(StoryEnding.True,true));
                var load=new AdventureSession(c);load.Restore(s.Story.CaptureCheckpoint());Require(load.Completed&&load.CurrentRoom.id=="M4_MILESTONE");
            });
            Directory.CreateDirectory("Assets/FabricatedDawn/Reports");
            File.WriteAllText("Assets/FabricatedDawn/Reports/rush-four-checks.json",JsonUtility.ToJson(report,true));AssetDatabase.Refresh();
            if(report.failed.Count>0)throw new InvalidOperationException(string.Join("\n",report.failed));
            Debug.Log("RUSH_FOUR_VERIFIED: "+report.passed.Count+" passed, 0 failed.");
            ChapterOneSetup.Verify();StoryDemoSetup.Verify();
        }

        public static AdventureSession BeforeChase(AdventureDefinition c)
        {
            var s=new AdventureSession(c);Act(s,"RUSH_INTRO_NEXT");Act(s,"M1_KEY_PICK");s.SelectItem("M1_KEY");Act(s,"M1_CABINET_USE");Act(s,"M1_NOTE_READ");Act(s,"M1_BED_OUT");Act(s,"M1_HALL_DOWN");
            foreach(var id in new[]{"M1_BIA","M1_BANG","M1_BAO"})Talk(s,id);
            foreach(var id in new[]{"M1_BOWL_LOOK","M1_LIVING_UP","M1_HALL_WC","M1_WASH","M1_MIRROR","M1_TURN","M1_HAND","M1_WC2_BACK","M1_HALL_SHRINE","M1_SHRINE_SIGN","M1_SHRINE_BACK","M1_HALL_BED","M1_SLEEP","M1_NIGHT_OUT","M1_FLOAT","M1_NIGHT_TO_WC","M1_NIGHT_WASH","M1_NIGHT_RETURN","M1_BALLOON_RETURN","M1_RETURN_BED","M1_NEXT_EVENING"})Act(s,id);
            Talk(s,"M1_END_GROUP");Act(s,"M1_TO_M2");Talk(s,"DEMO_GAME_BEGIN");for(int n=1;n<=4;n++)Act(s,"DEMO_CORNER_HOTSPOT_"+n);Act(s,"DEMO_RUN");return s;
        }
        public static AdventureSession Morning(AdventureDefinition c){var s=BeforeChase(c);Act(s,"DEMO_ESCAPE_UP");Act(s,"DEMO_ESCAPE_SHRINE");Talk(s,"RUSH_MANAGER");Act(s,"RUSH_TO_M3");return s;}
        public static AdventureSession Loop(AdventureDefinition c)
        {
            var s=Morning(c);Act(s,"M3_READ_LEASE");Act(s,"M3_READ_PHOTO");Act(s,"M3_HOME_PHONE");Talk(s,"M3_CALL_FAMILY");Talk(s,"M3_CALL_GRANDMA");Act(s,"M3_PHONE_BACK");Act(s,"M3_HOME_STREET");Talk(s,"M3_ASK_NEIGHBOR");Act(s,"M3_STREET_SHOP");Talk(s,"M3_ASK_ELDER");
            foreach(var id in new[]{"M3_TAKE_WARD","M3_BUY_INCENSE","M3_BUY_MATCHES","M3_BUY_BATTERIES","M3_SHOP_BACK","M3_STREET_HOME","M3_HOME_BOARD","M3_FACT_CRIME","M3_FACT_HIDDEN","M3_FACT_KIN","M3_COMPARE","M3_BOARD_BACK","M3_HOME_PACK","M3_TAKE_SIDEKEY"})Act(s,id);
            Talk(s,"M3_PACK_GROUP");Act(s,"M3_LEAVE");return s;
        }
        public static AdventureSession Side(AdventureDefinition c){var s=Loop(c);Act(s,"M4_MARK_A");Act(s,"M4_STAIRS_A");Act(s,"M4_MARK_B");Act(s,"M4_ROUTE_B");return s;}
        public static AdventureSession Past(AdventureDefinition c){var s=Side(c);s.SelectItem("RUSH_SIDEKEY");Act(s,"M4_UNLOCK");Act(s,"M4_OPEN_DOOR");return s;}
        static InteractionResult Act(AdventureSession s,string id){var r=s.Interact(id);Require(r.succeeded,id+": "+r.message);return r;}
        static void Talk(AdventureSession s,string id){Act(s,id);while(s.ActiveDialogue!=null)s.AdvanceDialogue();}
        static void Require(bool condition,string text="Rush demo assertion failed"){if(!condition)throw new InvalidOperationException(text);}
        static void Check(VerificationReport r,string name,Action check){try{check();r.passed.Add(name);}catch(Exception e){r.failed.Add(name+": "+e.Message);}}
    }
}

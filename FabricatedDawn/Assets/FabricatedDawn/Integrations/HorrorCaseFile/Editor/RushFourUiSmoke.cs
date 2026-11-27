using System;
using System.IO;
using System.Linq;
using FabricatedDawn.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FabricatedDawn.CaseFile.Editor
{
    // Editor-only UI regression tour; never ships in a player build.
    [InitializeOnLoad]
    public static class RushFourUiSmoke
    {
        const string Flag="FabricatedDawn.RushUiTour";
        static int phase;static double next;
        static VerificationReport report;
        static Action afterCapture;
        static readonly string Output=Path.Combine(Path.GetTempPath(),"FabricatedDawn-RushUI");
        static RushFourUiSmoke(){EditorApplication.playModeStateChanged+=OnPlay;}
        [MenuItem("Tools/Fabricated Dawn/Demo Màn 1–4/Kiểm tra giao diện trong Play")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Dừng Play trước khi kiểm tra giao diện.");
            EditorSceneManager.OpenScene(RushFourSetup.ScenePath);
            SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
        }
        static void OnPlay(PlayModeStateChange mode)
        {
            if(mode==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){
                phase=0;report=new VerificationReport{unityVersion=Application.unityVersion};Directory.CreateDirectory(Output);
                EditorApplication.ExecuteMenuItem("Window/General/Game");next=EditorApplication.timeSinceStartup+3;EditorApplication.update+=Tick;
            }
            if(mode==PlayModeStateChange.ExitingPlayMode)Stop();
        }
        static void Stop(){SessionState.SetBool(Flag,false);EditorApplication.update-=Tick;afterCapture=null;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1.2;
            var ui=UnityEngine.Object.FindAnyObjectByType<CaseFileAdventureUI>();
            if(ui==null||ui.Session==null)return;
            var root=ui.GetComponent<UIDocument>().rootVisualElement;
            try{
                if(afterCapture!=null){var action=afterCapture;afterCapture=null;action();return;}
                switch(phase){
                    case 0:ui.Show(CaseFileScreen.Hud);break;
                    case 1:
                        Hit(root,"RUSH_INTRO_NEXT");var text=root.Q<Label>(className:"fd-room-narration");
                        Require(text.worldBound.yMax<root.Q<Button>("RUSH_INTRO_NEXT").worldBound.yMin,"Opening overlaps start button");Shot("01-opening");break;
                    case 2:
                        ui.Session.Interact("RUSH_INTRO_NEXT");ui.Session.Interact("M1_KEY_PICK");ui.Session.SelectItem("M1_KEY");ui.Session.Interact("M1_CABINET_USE");ui.Session.Interact("M1_NOTE_READ");ui.Session.Interact("M1_BED_OUT");ui.Session.Interact("M1_HALL_DOWN");ui.Show(CaseFileScreen.Hud);break;
                    case 3:foreach(var id in new[]{"M1_BIA","M1_BANG","M1_BAO","M1_BOWL_LOOK"})Hit(root,id);ui.Interact("M1_BIA");break;
                    case 4:DialogFits(root);Shot("02-dialogue",()=>ClickLast(root));break;
                    case 5:DialogFits(root);ClickLast(root);break;
                    case 6:Require(ui.Session.ActiveDialogue==null,"Dialogue did not finish");ui.Session.Restore(RushFourVerification.Morning(ui.content).Story.CaptureCheckpoint());ui.Show(CaseFileScreen.Hud);break;
                    case 7:foreach(var id in new[]{"M3_READ_LEASE","M3_READ_PHOTO","M3_HOME_BOARD","M3_HOME_PHONE","M3_HOME_PACK"})Hit(root,id);ui.Interact("M3_READ_LEASE");break;
                    case 8:DialogFits(root);Shot("03-evidence",()=>ClickLast(root));break;
                    case 9:ui.Session.Restore(RushFourVerification.Loop(ui.content).Story.CaptureCheckpoint());ui.Show(CaseFileScreen.Hud);break;
                    case 10:foreach(var id in new[]{"M4_MARK_A","M4_STAIRS_A","M4_ROUTE_A"})Hit(root,id);Shot("04-loop",()=>{ui.Session.Restore(RushFourVerification.Side(ui.content).Story.CaptureCheckpoint());ui.Show(CaseFileScreen.Hud);});break;
                    case 11:ui.Session.SelectItem("RUSH_SIDEKEY");ui.Interact("M4_UNLOCK");break;
                    case 12:Hit(root,"M4_OPEN_DOOR");ui.Interact("M4_OPEN_DOOR");break;
                    case 13:Require(root.Q(className:"fd-door-transition")!=null,"Missing global change transition");DialogFits(root);Shot("05-door-transition",()=>ClickLast(root));break;
                    case 14:Require(ui.Session.Story.State.world==WorldPhase.PastEnclosed,"World has not changed");foreach(var id in new[]{"M4_CHECK_EXIT","M4_CHECK_WINDOW","M4_PAST_UP"})Hit(root,id);Shot("06-old-house",()=>ui.Show(CaseFileScreen.Inventory));break;
                    case 15:Require(root.Query<Button>(className:"fd-item").ToList().Count==ui.Session.Story.State.inventory.Count,"Inventory layout lost items");Shot("07-inventory",()=>ui.Show(CaseFileScreen.Journal));break;
                    case 16:Shot("08-journal",()=>{ui.Session.Restore(new AdventureSession(ui.content).Story.CaptureCheckpoint());ui.Session.Interact("RUSH_INTRO_NEXT");ui.Session.Interact("M1_KEY_PICK");ui.Session.SelectItem("M1_KEY");ui.Session.Interact("M1_CABINET_USE");ui.Session.Interact("M1_NOTE_READ");ui.Session.Interact("M1_BED_OUT");ui.Show(CaseFileScreen.Hud);});break;
                    case 17:
                        var stairs=root.Q<Button>("M1_HALL_DOWN");var label=stairs.Q<Label>(className:"fd-exit-label");
                        var picked=root.panel.Pick(label.worldBound.center);while(picked!=null&&picked!=stairs)picked=picked.parent;
                        Require(picked==stairs,"Inventory empty area blocks the stairs label");
                        ui.Session.Restore(new AdventureSession(ui.content).Story.CaptureCheckpoint());ui.Show(CaseFileScreen.MainMenu);break;
                    default:File.WriteAllText(Path.Combine(Output,"ui-checks.json"),JsonUtility.ToJson(report,true));Debug.Log("RUSH_UI_VERIFIED: "+report.passed.Count+" phases passed. "+Output);Stop();return;
                }
                report.passed.Add("UI phase "+phase);phase++;
            }catch(Exception ex){report.failed.Add("UI phase "+phase+": "+ex.Message);File.WriteAllText(Path.Combine(Output,"ui-checks.json"),JsonUtility.ToJson(report,true));Debug.LogException(ex);Stop();}
        }
        static void Hit(VisualElement root,string id)
        {
            var b=root.Q<Button>(id);Require(b!=null,"Missing button "+id);var point=b.worldBound.center;
            var picked=root.panel.Pick(point);while(picked!=null&&picked!=b)picked=picked.parent;
            Require(picked==b,"Blocked hotspot "+id);
        }
        static void DialogFits(VisualElement root)
        {
            var card=root.Q(className:"fd-card");var body=card?.Q<Label>(className:"fd-dialogue-body");var button=card?.Query<Button>().ToList().LastOrDefault();
            Require(card!=null&&body!=null&&button!=null,"Missing dialogue UI");
            Require(body.worldBound.yMax<=button.worldBound.yMin+2,"Text overlaps continue button");
            Require(button.worldBound.yMax<=card.worldBound.yMax+2&&card.worldBound.xMin>=root.worldBound.xMin&&card.worldBound.xMax<=root.worldBound.xMax+2,"Dialogue outside screen");
        }
        static void ClickLast(VisualElement root)
        {
            var button=root.Q(className:"fd-card").Query<Button>().ToList().Last();button.Focus();
            using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
        }
        static void Shot(string name,Action after=null){ScreenCapture.CaptureScreenshot(Path.Combine(Output,name+".png"));afterCapture=after;}
        static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
    }
}

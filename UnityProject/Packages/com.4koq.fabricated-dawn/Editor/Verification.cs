using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FabricatedDawn.Editor
{
    [Serializable]
    public sealed class VerificationReport
    {
        public string unityVersion;
        public List<string> passed = new List<string>();
        public List<string> failed = new List<string>();
    }

    public static class Verification
    {
        public static VerificationReport Run()
        {
            var report = new VerificationReport { unityVersion = Application.unityVersion };
            Check(report, "M4 door is gated and does not mutate early", () =>
            {
                var s = At(4); var before = s.CaptureCheckpoint();
                Require(!s.OpenPastDoor() && s.CaptureCheckpoint() == before);
                s.SolvePuzzle("M4_LOOP"); Require(!s.OpenPastDoor());
            });
            Check(report, "M4 switches once and preserves sanctuary and clues", () =>
            {
                var s = At(4); s.RecordClue("BOWL"); UnlockDoor(s);
                Require(s.OpenPastDoor() && s.State.world == WorldPhase.PastEnclosed);
                Require(s.State.sanctuary == SanctuaryPhase.Active && s.State.clues.Contains("BOWL"));
                Require(!s.OpenPastDoor() && s.State.completedEvents.Count == 1);
            });
            Check(report, "Door cannot trigger in another chapter", () =>
            { var s = At(3); UnlockDoor(s); Require(!s.OpenPastDoor()); });
            Check(report, "Past space blocks ordinary outside exits", () =>
            {
                var s = Past(5); s.State.unlockedRooms.Add("STAIRS_01");
                Require(!s.TryTravel("STAIRS_01", true));
                Require(s.TryTravel("STAIRS_01", false));
                Require(!s.TryTravel("UNKNOWN", false));
            });
            Check(report, "M6 dream is required; broken sanctuary cannot reset", () =>
            {
                var s = Past(6); Require(!s.BreakSanctuary()); s.SolvePuzzle("M6_DREAM");
                Require(s.BreakSanctuary() && s.State.sanctuary == SanctuaryPhase.Broken);
                Require(!s.BreakSanctuary() && s.State.world == WorldPhase.PastEnclosed);
            });
            Check(report, "Sanctuary cannot break at M4", () =>
            { var s = Past(4); s.SolvePuzzle("M6_DREAM"); Require(!s.BreakSanctuary()); });
            Check(report, "Wrong item target never consumes resources", () =>
            {
                var item = MakeItem("MATCHES");
                try { var s = At(3); Require(s.Collect(item));
                    Require(!s.TryUseItem(item.id, false, true) && s.State.inventory[0].remainingUses == 1);
                    Require(s.TryUseItem(item.id, true, true) && s.State.inventory.Count == 0);
                    Require(!s.Collect(item)); // A revisited pickup does not respawn a spent item.
                } finally { UnityEngine.Object.DestroyImmediate(item); }
            });
            Check(report, "Memory knife only affects the two adult spirits in past space", () =>
            {
                var knife = MakeItem("ITEM_MEMORY_KNIFE", true);
                try { var s = At(7); Require(!s.Collect(knife)); s = Past(7); Require(s.Collect(knife));
                    Require(s.TryKnife(Spirit.Wife) && s.TryKnife(Spirit.Husband) && !s.TryKnife(Spirit.Child));
                    Require(!s.TryKnife((Spirit)999));
                    Require(s.State.inventory[0].remainingUses == 1);
                } finally { UnityEngine.Object.DestroyImmediate(knife); }
            });
            Check(report, "Checkpoints restore every state field without aliases", () =>
            {
                var s = Past(7); var knife = MakeItem("ITEM_MEMORY_KNIFE", true);
                try { s.Collect(knife); } finally { UnityEngine.Object.DestroyImmediate(knife); }
                s.State.roomId = "STUDY_01"; s.State.sanctuary = SanctuaryPhase.Broken;
                s.State.childTrust = 3; s.State.husbandAttitude = -2; s.State.childHarmed = true;
                s.State.unlockedRooms.Add("STUDY_01"); s.RecordClue("A"); s.SolvePuzzle("B");
                s.RecordChoice("C"); s.CompleteEvent("D"); s.State.protectionCharges = 2;
                var saved = s.CaptureCheckpoint(); var oldState = s.State;
                s.State.inventory.Clear(); s.State.clues.Clear(); s.TryProtection(); s.State.chapter = 8;
                s.RestoreCheckpoint(saved);
                Require(s.CaptureCheckpoint() == saved && !ReferenceEquals(oldState, s.State));
                oldState.choices.Clear(); Require(s.State.choices.Count == 1);
            });
            Check(report, "Malformed and future checkpoints fail without changing current state", () =>
            {
                var s = At(3); var before = s.CaptureCheckpoint();
                foreach (var json in new[] { "{}", "garbage", "{\"schemaVersion\":2}", before.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 999") })
                {
                    bool rejected = false;
                    try { s.RestoreCheckpoint(json); } catch (ArgumentException) { rejected = true; }
                    Require(rejected && s.CaptureCheckpoint() == before);
                }
            });
            Check(report, "One-shot events do not repeat", () =>
            { var s = At(1); Require(s.CompleteEvent("BALLOON") && !s.CompleteEvent("BALLOON")); });
            Check(report, "Death retry restores charm charges and is not a Bad Ending", () =>
            {
                var s = At(2); var saved = s.CaptureCheckpoint();
                Require(s.TryProtection() && !s.TryProtection()); s.RestoreCheckpoint(saved);
                Require(s.State.protectionCharges == 1 && s.State.ending == StoryEnding.None);
            });
            Check(report, "Good Ending requires prior evidence and child relationship", () =>
            {
                var s = ReadyFinale(); Require(!s.CanChooseEnding(StoryEnding.Good, false));
                AddFamilyEvidence(s); Require(s.CanChooseEnding(StoryEnding.Good, false));
                s.State.childHarmed = true; Require(!s.CanChooseEnding(StoryEnding.Good, false));
            });
            Check(report, "True Ending stays locked until narrative approval AND friend evidence", () =>
            {
                var s = ReadyFinale(); AddFamilyEvidence(s);
                Require(!s.CanChooseEnding(StoryEnding.True, true)); s.RecordClue("EVIDENCE_FRIEND");
                Require(!s.CanChooseEnding(StoryEnding.True, false) && s.CanChooseEnding(StoryEnding.True, true));
            });
            Check(report, "Escape drops memory objects but retains physical items and journal", () =>
            {
                var s = ReadyFinale(); AddFamilyEvidence(s);
                var knife = MakeItem("ITEM_MEMORY_KNIFE", true); var papers = MakeItem("PAPERS");
                try { s.Collect(knife); s.Collect(papers); }
                finally { UnityEngine.Object.DestroyImmediate(knife); UnityEngine.Object.DestroyImmediate(papers); }
                Require(s.ChooseEnding(StoryEnding.Good, false, true));
                Require(s.State.world == WorldPhase.Present && s.State.inventory.Count == 1 &&
                    s.State.inventory[0].id == "PAPERS" && s.State.clues.Contains("EVIDENCE_FAMILY"));
                Require(!s.ChooseEnding(StoryEnding.Bad, false, false));
                CheckpointCodec.Validate(s.State);
            });
            Check(report, "Validator catches duplicate IDs and invalid destinations", () =>
            {
                var catalog = ScriptableObject.CreateInstance<ProjectCatalog>();
                var room = ScriptableObject.CreateInstance<RoomDefinition>();
                try { room.id = "ROOM"; room.presentHotspots = new[] {
                    new HotspotDefinition { id = "ROOM", destinationRoomId = "MISSING", bounds = new Rect(-1, 0, 2, 1) } };
                    catalog.rooms = new[] { room }; var r = ContentValidator.Inspect(catalog);
                    Require(r.errors.Exists(x => x.Contains("duplicate")) && r.errors.Exists(x => x.Contains("destination")) &&
                        r.errors.Exists(x => x.Contains("bounds")));
                } finally { UnityEngine.Object.DestroyImmediate(room); UnityEngine.Object.DestroyImmediate(catalog); }
            });
            return report;
        }

        private static void Check(VerificationReport report, string name, Action action)
        {
            try { action(); report.passed.Add(name); }
            catch (Exception ex) { report.failed.Add(name + ": " + ex.Message); }
        }
        private static void Require(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
        private static StorySession At(int chapter) => new StorySession(new StoryState { chapter = chapter });
        private static StorySession Past(int chapter) => new StorySession(new StoryState { chapter = chapter, world = WorldPhase.PastEnclosed });
        private static void UnlockDoor(StorySession s) { s.SolvePuzzle("M4_LOOP"); s.SolvePuzzle("M4_SIDE_DOOR"); }
        private static ItemDefinition MakeItem(string id, bool pastOnly = false)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>(); item.id = id; item.pastOnly = pastOnly;
            item.origin = pastOnly ? ItemOrigin.Linked : ItemOrigin.Physical; return item;
        }
        private static StorySession ReadyFinale()
        { var s = Past(8); s.CompleteEvent("M8_CONFRONTATION_DONE"); return s; }
        private static void AddFamilyEvidence(StorySession s)
        {
            s.RecordClue("EVIDENCE_FAMILY"); s.RecordClue("EVIDENCE_REAL_WORLD");
            s.SolvePuzzle("M7_THREE_DEATHS"); s.CompleteEvent("M7_KNIFE_LEARNED"); s.State.childTrust = 1;
        }
    }

    public static class Batch
    {
        public static void Scaffold()
        {
            ProjectScaffolder.Create(); ProjectScaffolder.CreateDemoScene();
            Validate();
        }

        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ProjectCatalog>(ProjectScaffolder.CatalogPath);
            var report = ContentValidator.Inspect(catalog);
            Write("content-report.json", JsonUtility.ToJson(report, true));
            if (report.errors.Count > 0) throw new Exception("Content validation failed; see Reports/content-report.json");
            Debug.Log("Fabricated Dawn content validation passed (placeholder warnings are expected).");
        }

        public static void Verify()
        {
            Scaffold();
            var first = File.ReadAllText(ProjectScaffolder.CatalogPath);
            ProjectScaffolder.Create();
            if (first != File.ReadAllText(ProjectScaffolder.CatalogPath)) throw new Exception("Scaffolding modified existing catalog.");
            var report = Verification.Run();
            Write("verification-report.json", JsonUtility.ToJson(report, true));
            if (report.failed.Count > 0) throw new Exception("Verification failed: " + string.Join("; ", report.failed));
            Debug.Log($"FABRICATED_DAWN_VERIFIED: {report.passed.Count} checks passed; scaffolding is idempotent.");
        }

        private static void Write(string filename, string text)
        {
            Directory.CreateDirectory(ProjectScaffolder.Root + "/Reports");
            File.WriteAllText(ProjectScaffolder.Root + "/Reports/" + filename, text);
            AssetDatabase.Refresh();
        }
    }
}

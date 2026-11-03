using System;
using System.IO;
using UnityEngine;

namespace FabricatedDawn.Editor
{
    public static class ToolkitVerification
    {
        public static VerificationReport Run(AdventureDefinition content)
        {
            var report = new VerificationReport { unityVersion = Application.unityVersion };
            Check(report, "Sandbox content validates", () => Require(AdventureValidator.Errors(content).Count == 0));
            Check(report, "Locked door leaves state unchanged", () =>
            {
                var s = New(content); var before = s.Story.CaptureCheckpoint();
                Require(!s.Interact("HP_EXIT").succeeded && s.Story.CaptureCheckpoint() == before);
            });
            Check(report, "Cannot interact with a hotspot in a different room", () =>
            { var s = New(content); Require(!s.Interact("HP_SIGN").succeeded && !s.Completed); });
            Check(report, "Pickup adds one item and cannot repeat", () =>
            {
                var s = New(content); Require(s.Interact("HP_KEY").succeeded);
                Require(!s.Interact("HP_KEY").succeeded && s.Story.State.inventory.Count == 1);
                Require(!s.SelectItem("NONEXISTENT"));
            });
            Check(report, "Wrong selected item preserves state and selection", () =>
            {
                var s = New(content); s.Interact("HP_STONE"); s.SelectItem("ITEM_SANDBOX_STONE");
                var before = s.Story.CaptureCheckpoint();
                Require(!s.Interact("HP_CABINET").succeeded && s.Story.CaptureCheckpoint() == before);
                Require(s.SelectedItemId == "ITEM_SANDBOX_STONE");
            });
            Check(report, "Using key opens cabinet, consumes once and reveals clue", () =>
            {
                var s = New(content); Require(!s.Interact("HP_NOTE").succeeded); OpenCabinet(s);
                Require(s.Story.State.inventory.Count == 0 && s.SelectedItemId == null);
                Require(s.Story.State.solvedPuzzles.Contains("PUZZLE_CABINET_OPEN"));
                Require(!s.Interact("HP_CABINET").succeeded && s.Interact("HP_NOTE").succeeded);
            });
            Check(report, "Clue reread does not duplicate journal or unlocks", () =>
            {
                var s = New(content); OpenCabinet(s); s.Interact("HP_NOTE"); s.Interact("HP_NOTE");
                Require(s.Story.State.clues.Count == 1 && s.Story.State.unlockedRooms.Count == 2);
            });
            Check(report, "Room revisit preserves puzzle and pickup state", () =>
            {
                var s = New(content); OpenCabinet(s); s.Interact("HP_NOTE");
                Require(s.Interact("HP_EXIT").succeeded && s.Story.State.roomId == "HALL_01");
                Require(s.Interact("HP_RETURN").succeeded && s.Story.State.roomId == "ROOM_01");
                Require(!s.Interact("HP_KEY").succeeded && !s.Interact("HP_CABINET").succeeded && s.Interact("HP_NOTE").succeeded);
            });
            Check(report, "Dialogue blocks world input until all lines are acknowledged", () =>
            {
                var s = New(content); Require(s.Interact("HP_FRIEND").succeeded);
                Require(!s.Interact("HP_KEY").succeeded && !s.SelectItem("ITEM_SANDBOX_KEY"));
                int lines = s.ActiveDialogue.lines.Length;
                for (int i = 0; i < lines; i++) Require(s.AdvanceDialogue());
                Require(s.ActiveDialogue == null && !s.AdvanceDialogue() && s.Interact("HP_KEY").succeeded);
                Require(s.Story.State.completedEvents.Contains("dialogue:DIALOGUE_SANDBOX_FRIEND"));
            });
            Check(report, "Full interaction loop reaches completion through the shared engine", () =>
            {
                var s = New(content); OpenCabinet(s); s.Interact("HP_NOTE"); s.Interact("HP_EXIT");
                Require(s.Interact("HP_SIGN").succeeded && s.Completed && s.Story.State.clues.Count == 2);
            });
            Check(report, "Disk checkpoint restores full state into a fresh session", () =>
            {
                var s = New(content); s.Interact("HP_STONE"); OpenCabinet(s); s.Interact("HP_NOTE"); s.Interact("HP_EXIT");
                s.Story.State.childTrust = 2; s.Story.State.husbandAttitude = -1;
                s.Story.RecordChoice("TEST_CHOICE"); s.Story.TryProtection();
                var expected = s.Story.CaptureCheckpoint(); var store = Store(content); store.Save(s);
                var fresh = New(content); store.Load(fresh);
                Require(fresh.Story.CaptureCheckpoint() == expected && fresh.SelectedItemId == null && fresh.ActiveDialogue == null);
            });
            Check(report, "Atomic replacement keeps a recoverable previous checkpoint", () =>
            {
                var s = New(content); var store = Store(content); store.Save(s);
                var previous = s.Story.CaptureCheckpoint(); OpenCabinet(s); store.Save(s);
                Require(File.Exists(store.FilePath + ".bak")); store.LoadBackup(s);
                Require(s.Story.CaptureCheckpoint() == previous);
            });
            Check(report, "Corrupted disk data is rejected without changing current state", () =>
            {
                var s = New(content); var store = Store(content); store.Save(s);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(store.FilePath));
                envelope.payload = envelope.payload.Replace("ROOM_01", "HALL_01");
                File.WriteAllText(store.FilePath, JsonUtility.ToJson(envelope));
                var before = s.Story.CaptureCheckpoint(); bool rejected = false;
                try { store.Load(s); } catch (ArgumentException) { rejected = true; }
                Require(rejected && s.Story.CaptureCheckpoint() == before);
            });
            Check(report, "Future save formats and wrong content IDs are rejected", () =>
            {
                var s = New(content); var store = Store(content); store.Save(s);
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(store.FilePath));
                envelope.formatVersion = 900;
                File.WriteAllText(store.FilePath, JsonUtility.ToJson(envelope));
                bool futureRejected = false;
                try { store.Load(s); } catch (ArgumentException) { futureRejected = true; }
                var wrong = new CheckpointStore(store.FilePath, "different-content"); bool wrongRejected = false;
                try { wrong.Save(s); } catch (ArgumentException) { wrongRejected = true; }
                Require(futureRejected && wrongRejected);
            });
            Check(report, "Unknown content references are rejected before replacing live state", () =>
            {
                var s = New(content); var before = s.Story.CaptureCheckpoint();
                var state = CheckpointCodec.Restore(before); state.roomId = "UNKNOWN"; state.unlockedRooms.Add("UNKNOWN");
                bool rejected = false;
                try { s.Restore(CheckpointCodec.Capture(state)); } catch (ArgumentException) { rejected = true; }
                Require(rejected && before == s.Story.CaptureCheckpoint());
            });
            Check(report, "Restoring a checkpoint clears temporary UI selection and dialogue", () =>
            {
                var s = New(content); s.Interact("HP_KEY"); var save = s.Story.CaptureCheckpoint();
                s.SelectItem("ITEM_SANDBOX_KEY"); s.Interact("HP_FRIEND"); s.Restore(save);
                Require(s.SelectedItemId == null && s.ActiveDialogue == null && s.DialogueLine == 0);
            });
            return report;
        }

        private static AdventureSession New(AdventureDefinition content) => new AdventureSession(content);
        private static void OpenCabinet(AdventureSession session)
        {
            Require(session.Interact("HP_KEY").succeeded);
            Require(session.SelectItem("ITEM_SANDBOX_KEY"));
            Require(session.Interact("HP_CABINET").succeeded);
        }
        private static CheckpointStore Store(AdventureDefinition content) => new CheckpointStore(
            Path.GetFullPath("Temp/ToolkitChecks/" + Guid.NewGuid().ToString("N") + "/checkpoint.json"), content.contentId);
        private static void Check(VerificationReport report, string name, Action action)
        { try { action(); report.passed.Add(name); } catch (Exception ex) { report.failed.Add(name + ": " + ex.Message); } }
        private static void Require(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
    }
}

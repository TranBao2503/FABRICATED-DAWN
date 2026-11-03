using UnityEngine;

namespace FabricatedDawn
{
    // A deliberate developer prototype of the M4 boundary, not a finished chapter.
    public sealed class DoorPrototype : MonoBehaviour
    {
        private StorySession session;
        private string checkpoint;
        private string feedback = "Observe the repeating corridor, find the side-door key, then open the door.";
        private Vector2 scroll;

        private void Awake() => ResetPrototype();

        private void ResetPrototype()
        {
            session = new StorySession(new StoryState { chapter = 4, roomId = "HALL_01" });
            checkpoint = session.CaptureCheckpoint();
        }

        private void OnGUI()
        {
            var old = GUI.matrix;
            var scale = Mathf.Min(Screen.width / 960f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 960 * scale) / 2,
                (Screen.height - 600 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            GUI.Box(new Rect(0, 0, 960, 600), "");
            var state = session.State;
            GUI.Label(new Rect(28, 15, 900, 30), "FABRICATED DAWN · M4 developer prototype · Placeholder visuals");
            GUI.Label(new Rect(28, 48, 900, 25), $"World: {state.world} | Shrine: {state.sanctuary} | Room: {state.roomId}");
            var color = GUI.color;
            GUI.color = state.world == WorldPhase.Present ? new Color(.4f, .55f, .42f) : new Color(.7f, .4f, .2f);
            GUI.Box(new Rect(28, 85, 900, 200), state.world == WorldPhase.Present
                ? "PRESENT: student belongings · fluorescent light · repeating corridor"
                : "PAST_ENCLOSED: old family furniture · toys · all room layers changed together");
            GUI.color = color;
            if (GUI.Button(new Rect(45, 220, 260, 45), "Observe the unchanged floor crack"))
            { session.SolvePuzzle("M4_LOOP"); feedback = "The landmark reveals the concealed path."; }
            if (GUI.Button(new Rect(330, 220, 260, 45), "Find key / unlock side door"))
            {
                if (state.solvedPuzzles.Contains("M4_LOOP"))
                { session.SolvePuzzle("M4_SIDE_DOOR"); feedback = "Side door unlocked. Traffic seems audible beyond it."; }
                else feedback = "Find a way through the corridor loop first.";
            }
            if (GUI.Button(new Rect(615, 220, 260, 45), "OPEN THE DOOR"))
                feedback = session.OpenPastDoor() ? "The WHOLE house changed at the doorway. Shrine protection is still ACTIVE."
                    : "Door unavailable: solve the loop and unlock it first; the transition cannot repeat.";
            if (GUI.Button(new Rect(28, 305, 210, 38), "Try outside exit"))
            {
                if (!state.unlockedRooms.Contains("STAIRS_01")) state.unlockedRooms.Add("STAIRS_01");
                feedback = session.TryTravel("STAIRS_01", true) ? "The present-day exit can be reached."
                    : "No ordinary door leads outside the enclosed memory space.";
            }
            if (GUI.Button(new Rect(250, 305, 210, 38), "Checkpoint"))
            { checkpoint = session.CaptureCheckpoint(); feedback = "Checkpoint captured, including all collections and protection state."; }
            if (GUI.Button(new Rect(472, 305, 210, 38), "Retry checkpoint"))
            { session.RestoreCheckpoint(checkpoint); feedback = "Restored the complete checkpoint snapshot."; }
            if (GUI.Button(new Rect(694, 305, 234, 38), "Reset developer prototype"))
            { ResetPrototype(); feedback = "Reset to M4, before opening the side door."; }
            GUI.Box(new Rect(28, 360, 900, 70), feedback);
            GUILayout.BeginArea(new Rect(28, 445, 900, 135));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Checkpoint JSON (for development)");
            GUILayout.TextArea(session.CaptureCheckpoint());
            GUILayout.EndScrollView(); GUILayout.EndArea();
            GUI.matrix = old;
        }
    }
}

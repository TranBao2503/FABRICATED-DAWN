using System;

namespace FabricatedDawn
{
    public sealed class InteractionResult
    {
        public bool succeeded;
        public bool checkpoint;
        public string message;
        public string clueId;
    }

    // UI, automated checks and future scene controllers share this interaction path.
    public sealed class AdventureSession
    {
        public AdventureDefinition Content { get; }
        public StorySession Story { get; }
        public string SelectedItemId { get; private set; }
        public DialogueDefinition ActiveDialogue { get; private set; }
        public int DialogueLine { get; private set; }
        public AdventureRoom CurrentRoom => Content.Room(Story.State.roomId);
        public bool Completed => Story.State.solvedPuzzles.Contains(Content.completionPuzzleId);
        public string Objective
        {
            get
            {
                if (Completed) return Content.completedObjective;
                string text = Content.initialObjective;
                foreach (var update in Content.objectiveUpdates)
                    if (update != null && Story.State.solvedPuzzles.Contains(update.afterPuzzle)) text = update.text;
                return text;
            }
        }

        public AdventureSession(AdventureDefinition content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            if (content.Room(content.startRoomId) == null) throw new ArgumentException("Unknown starting room.");
            var initial = new StoryState { roomId = content.startRoomId };
            initial.unlockedRooms.Clear(); initial.unlockedRooms.Add(content.startRoomId);
            Story = new StorySession(initial);
        }

        public bool SelectItem(string id)
        {
            if (ActiveDialogue != null || !Story.State.inventory.Exists(x => x.id == id)) return false;
            SelectedItemId = SelectedItemId == id ? null : id;
            return true;
        }

        public bool IsVisible(AdventureHotspot h)
        {
            if (h == null) return false;
            var state = Story.State;
            return (string.IsNullOrEmpty(h.visibleAfterPuzzle) || state.solvedPuzzles.Contains(h.visibleAfterPuzzle)) &&
                (string.IsNullOrEmpty(h.hiddenAfterPuzzle) || !state.solvedPuzzles.Contains(h.hiddenAfterPuzzle)) &&
                (string.IsNullOrEmpty(h.hiddenAfterEvent) || !state.completedEvents.Contains(h.hiddenAfterEvent)) &&
                !(h.kind == InteractionKind.Pickup && h.item != null && state.completedEvents.Contains("pickup:" + h.item.id));
        }

        public InteractionResult Interact(string id)
        {
            if (ActiveDialogue != null) return Fail("Hãy kết thúc hội thoại trước.");
            var h = Array.Find(CurrentRoom.hotspots, x => x != null && x.id == id);
            if (h == null || !IsVisible(h)) return Fail("Không thể tương tác ở đây.");
            if (!string.IsNullOrEmpty(h.requiredPuzzle) && !Story.State.solvedPuzzles.Contains(h.requiredPuzzle))
                return Fail(string.IsNullOrEmpty(h.lockedResponse) ? "Chưa đủ điều kiện." : h.lockedResponse);
            switch (h.kind)
            {
                case InteractionKind.Pickup:
                    if (!Story.Collect(h.item)) return Fail("Vật phẩm không còn ở đây.");
                    break;
                case InteractionKind.UseItem:
                    if (h.item == null || SelectedItemId != h.item.id ||
                        !Story.TryUseItem(h.item.id, true, h.consumeItem))
                        return Fail(string.IsNullOrEmpty(SelectedItemId) ? "Chọn vật phẩm trong túi rồi nhấn vào đồ vật."
                            : "Vật phẩm này không phù hợp. Bạn vẫn giữ nguyên vật phẩm.");
                    SelectedItemId = null;
                    break;
                case InteractionKind.ReadClue:
                    if (Content.Clue(h.clueId) == null) return Fail("Thiếu dữ liệu manh mối.");
                    Story.RecordClue(h.clueId);
                    break;
                case InteractionKind.Talk:
                    var dialogue = Content.Dialogue(h.dialogueId);
                    if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0) return Fail("Thiếu hội thoại.");
                    ActiveDialogue = dialogue; DialogueLine = 0;
                    break;
                case InteractionKind.Travel:
                    if (Content.Room(h.destinationRoom) == null || !Story.TryTravel(h.destinationRoom, false))
                        return Fail(string.IsNullOrEmpty(h.lockedResponse) ? "Lối đi chưa mở." : h.lockedResponse);
                    SelectedItemId = null;
                    break;
            }
            if (!string.IsNullOrEmpty(h.solvesPuzzle)) Story.SolvePuzzle(h.solvesPuzzle);
            if (!string.IsNullOrEmpty(h.unlocksRoom) && !Story.State.unlockedRooms.Contains(h.unlocksRoom))
                Story.State.unlockedRooms.Add(h.unlocksRoom);
            Story.CompleteEvent("interaction:" + h.id);
            return new InteractionResult { succeeded = true, checkpoint = h.checkpointAfter,
                message = h.response, clueId = h.kind == InteractionKind.ReadClue ? h.clueId : null };
        }

        public bool AdvanceDialogue()
        {
            if (ActiveDialogue == null) return false;
            DialogueLine++;
            if (DialogueLine >= ActiveDialogue.lines.Length)
            {
                Story.CompleteEvent("dialogue:" + ActiveDialogue.id);
                ActiveDialogue = null; DialogueLine = 0;
            }
            return true;
        }

        public void Restore(string json)
        {
            var candidate = CheckpointCodec.Restore(json);
            ValidateContentState(candidate);
            Story.RestoreCheckpoint(json);
            SelectedItemId = null; ActiveDialogue = null; DialogueLine = 0;
        }

        public void ValidateContentState(StoryState state)
        {
            if (Content.Room(state.roomId) == null || !state.unlockedRooms.Contains(state.roomId))
                throw new ArgumentException("Save refers to an unknown or locked current room.");
            foreach (var room in state.unlockedRooms)
                if (Content.Room(room) == null) throw new ArgumentException("Unknown room in save: " + room);
            foreach (var entry in state.inventory)
                if (Content.Item(entry.id) == null) throw new ArgumentException("Unknown item in save: " + entry.id);
            foreach (var clue in state.clues)
                if (Content.Clue(clue) == null) throw new ArgumentException("Unknown clue in save: " + clue);
        }

        private static InteractionResult Fail(string message) => new InteractionResult { message = message };
    }
}

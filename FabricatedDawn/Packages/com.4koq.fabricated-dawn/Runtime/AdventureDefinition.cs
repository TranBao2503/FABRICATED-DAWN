using System;
using UnityEngine;

namespace FabricatedDawn
{
    public enum InteractionKind { Inspect, Pickup, UseItem, ReadClue, Talk, Travel }

    [Serializable]
    public sealed class AdventureHotspot
    {
        public string id;
        public string label;
        public InteractionKind kind;
        public Rect bounds = new Rect(.1f, .1f, .15f, .15f);
        [Tooltip("Optional CSS class for placeholder presentation.")]
        public string visualClass;
        public string requiredPuzzle;
        public string visibleAfterPuzzle;
        public string hiddenAfterPuzzle;
        public string hiddenAfterEvent;
        public ItemDefinition item;
        public bool consumeItem;
        public string clueId;
        public string dialogueId;
        public string destinationRoom;
        public string solvesPuzzle;
        public string unlocksRoom;
        [TextArea] public string response;
        [TextArea] public string lockedResponse;
        public bool checkpointAfter;
    }

    [Serializable]
    public sealed class AdventureRoom
    {
        public string id;
        public string title;
        public string subtitle;
        [TextArea] public string description;
        public Color placeholderColor = new Color(.22f, .28f, .24f);
        public Sprite presentBackground;
        public Sprite pastBackground;
        public AdventureHotspot[] hotspots = Array.Empty<AdventureHotspot>();
    }

    [Serializable]
    public sealed class ClueDefinition
    {
        public string id;
        public string title;
        [TextArea(3, 10)] public string body;
    }

    [Serializable]
    public sealed class DialogueDefinition
    {
        public string id;
        public string speaker;
        [TextArea(2, 5)] public string[] lines = Array.Empty<string>();
    }

    [CreateAssetMenu(menuName = "Fabricated Dawn/Adventure")]
    public sealed class AdventureDefinition : ScriptableObject
    {
        public string contentId = "toolkit-sandbox-v1";
        public string startRoomId = "ROOM_01";
        [TextArea] public string initialObjective;
        public AdventureObjective[] objectiveUpdates = Array.Empty<AdventureObjective>();
        [TextArea] public string completedObjective = "Hoàn tất cảnh thử. Bạn có thể quay lại hoặc tải checkpoint.";
        public string completionPuzzleId;
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
        public AdventureRoom[] rooms = Array.Empty<AdventureRoom>();
        public ClueDefinition[] clues = Array.Empty<ClueDefinition>();
        public DialogueDefinition[] dialogues = Array.Empty<DialogueDefinition>();

        public AdventureRoom Room(string id) => Array.Find(rooms, x => x != null && x.id == id);
        public ItemDefinition Item(string id) => Array.Find(items, x => x != null && x.id == id);
        public ClueDefinition Clue(string id) => Array.Find(clues, x => x != null && x.id == id);
        public DialogueDefinition Dialogue(string id) => Array.Find(dialogues, x => x != null && x.id == id);
    }

    [Serializable]
    public sealed class AdventureObjective
    {
        public string afterPuzzle;
        [TextArea] public string text;
    }
}

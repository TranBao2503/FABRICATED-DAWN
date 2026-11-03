using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabricatedDawn
{
    public enum WorldPhase { Present, PastEnclosed }
    public enum SanctuaryPhase { Active, Broken }
    public enum ItemOrigin { Physical, Memory, Linked }
    public enum Spirit { Wife, Husband, Child }
    public enum StoryEnding { None, Bad, Good, True }

    [Serializable]
    public sealed class InventoryEntry
    {
        public string id;
        public ItemOrigin origin;
        public bool pastOnly;
        public int remainingUses;
    }

    // Serializable lists are intentional: Unity JsonUtility does not serialize dictionaries.
    [Serializable]
    public sealed class StoryState
    {
        public int schemaVersion = 1;
        public int chapter = 1;
        public string roomId = "ROOM_01";
        public WorldPhase world;
        public SanctuaryPhase sanctuary;
        public int childTrust;
        public int husbandAttitude;
        public int protectionCharges = 1;
        public bool childHarmed;
        public StoryEnding ending;
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public List<string> clues = new List<string>();
        public List<string> solvedPuzzles = new List<string>();
        public List<string> completedEvents = new List<string>();
        public List<string> unlockedRooms = new List<string> { "ROOM_01" };
        public List<string> choices = new List<string>();
    }

    public static class CheckpointCodec
    {
        public static string Capture(StoryState state)
        {
            Validate(state);
            return JsonUtility.ToJson(state, true);
        }

        public static StoryState Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Empty checkpoint.");
            // Do not allow {} to silently become a fresh state with default field values.
            string[] required = { "schemaVersion", "chapter", "roomId", "world", "sanctuary",
                "childTrust", "husbandAttitude", "protectionCharges", "childHarmed", "ending",
                "inventory", "clues", "solvedPuzzles", "completedEvents", "unlockedRooms", "choices" };
            foreach (var key in required)
                if (!json.Contains("\"" + key + "\"")) throw new ArgumentException("Missing checkpoint field: " + key);
            var restored = JsonUtility.FromJson<StoryState>(json);
            Validate(restored);
            return restored;
        }

        public static void Validate(StoryState state)
        {
            if (state == null || state.schemaVersion != 1 || state.chapter < 1 || state.chapter > 8 ||
                string.IsNullOrWhiteSpace(state.roomId) || state.protectionCharges < 0 ||
                !Enum.IsDefined(typeof(WorldPhase), state.world) ||
                !Enum.IsDefined(typeof(SanctuaryPhase), state.sanctuary) ||
                !Enum.IsDefined(typeof(StoryEnding), state.ending))
                throw new ArgumentException("Unsupported or invalid checkpoint state.");
            ValidateIds(state.clues); ValidateIds(state.solvedPuzzles);
            ValidateIds(state.completedEvents); ValidateIds(state.unlockedRooms); ValidateIds(state.choices);
            if (state.inventory == null) throw new ArgumentException("Missing inventory.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.inventory)
                if (item == null || string.IsNullOrWhiteSpace(item.id) || !seen.Add(item.id) ||
                    item.remainingUses < 1 || !Enum.IsDefined(typeof(ItemOrigin), item.origin) ||
                    (state.world == WorldPhase.Present && item.pastOnly))
                    throw new ArgumentException("Invalid inventory entry.");
        }

        private static void ValidateIds(List<string> ids)
        {
            if (ids == null) throw new ArgumentException("Missing state collection.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    throw new ArgumentException("Empty or duplicate state ID.");
        }
    }

    public sealed class StorySession
    {
        public StoryState State { get; private set; }
        public StorySession() : this(new StoryState()) { }
        public StorySession(StoryState state)
        {
            // Own a deep snapshot so callers cannot retain an alias to the input state.
            State = CheckpointCodec.Restore(CheckpointCodec.Capture(state));
        }

        public bool RecordClue(string id) => AddUnique(State.clues, id);
        public bool SolvePuzzle(string id) => AddUnique(State.solvedPuzzles, id);
        public bool CompleteEvent(string id) => AddUnique(State.completedEvents, id);
        public bool RecordChoice(string id) => AddUnique(State.choices, id);
        public string CaptureCheckpoint() => CheckpointCodec.Capture(State);
        public void RestoreCheckpoint(string json)
        {
            var restored = CheckpointCodec.Restore(json);
            State = restored; // Replace only after parsing and validation have both succeeded.
        }

        public bool Collect(ItemDefinition item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || item.uses < 1 ||
                (item.pastOnly && State.world != WorldPhase.PastEnclosed) ||
                State.inventory.Exists(x => x.id == item.id) ||
                State.completedEvents.Contains("pickup:" + item.id)) return false;
            State.inventory.Add(new InventoryEntry { id = item.id, origin = item.origin,
                pastOnly = item.pastOnly, remainingUses = item.uses });
            CompleteEvent("pickup:" + item.id);
            return true;
        }

        // Interaction code validates its target first. Failed interactions never spend a charge.
        public bool TryUseItem(string id, bool targetAccepts, bool consume)
        {
            if (!targetAccepts) return false;
            var item = State.inventory.Find(x => x.id == id);
            if (item == null || (item.pastOnly && State.world != WorldPhase.PastEnclosed)) return false;
            if (consume && --item.remainingUses == 0) State.inventory.Remove(item);
            return true;
        }

        public bool OpenPastDoor()
        {
            if (State.chapter != 4 || State.world != WorldPhase.Present ||
                !State.solvedPuzzles.Contains("M4_LOOP") ||
                !State.solvedPuzzles.Contains("M4_SIDE_DOOR")) return false;
            State.world = WorldPhase.PastEnclosed;
            State.roomId = "ROOM_01";
            CompleteEvent("M4_DOOR_OPENED");
            return true;
        }

        public bool BreakSanctuary()
        {
            if (State.chapter != 6 || State.world != WorldPhase.PastEnclosed ||
                State.sanctuary != SanctuaryPhase.Active ||
                !State.solvedPuzzles.Contains("M6_DREAM")) return false;
            State.sanctuary = SanctuaryPhase.Broken;
            CompleteEvent("M6_SANCTUARY_BROKEN");
            return true;
        }

        public bool TryTravel(string roomId, bool leadsOutside)
        {
            if (State.ending != StoryEnding.None || string.IsNullOrWhiteSpace(roomId) ||
                (leadsOutside && State.world == WorldPhase.PastEnclosed) ||
                !State.unlockedRooms.Contains(roomId)) return false;
            State.roomId = roomId;
            return true;
        }

        public bool TryKnife(Spirit target)
        {
            return (target == Spirit.Wife || target == Spirit.Husband) && State.world == WorldPhase.PastEnclosed &&
                TryUseItem("ITEM_MEMORY_KNIFE", true, false);
        }

        public bool TryProtection()
        {
            if (State.protectionCharges < 1) return false;
            State.protectionCharges--;
            return true;
        }

        // Eligibility is a provisional implementation policy, not a finalized narrative design.
        public bool CanChooseEnding(StoryEnding ending, bool trueRouteApproved)
        {
            if (State.chapter != 8 || !State.completedEvents.Contains("M8_CONFRONTATION_DONE")) return false;
            if (ending == StoryEnding.Bad) return true;
            bool family = !State.childHarmed && State.childTrust > 0 &&
                State.clues.Contains("EVIDENCE_FAMILY") && State.clues.Contains("EVIDENCE_REAL_WORLD") &&
                State.solvedPuzzles.Contains("M7_THREE_DEATHS") &&
                State.completedEvents.Contains("M7_KNIFE_LEARNED");
            if (ending == StoryEnding.Good) return family;
            return ending == StoryEnding.True && family && trueRouteApproved &&
                State.clues.Contains("EVIDENCE_FRIEND");
        }

        public bool ChooseEnding(StoryEnding ending, bool trueRouteApproved, bool escapes)
        {
            if (State.ending != StoryEnding.None || !CanChooseEnding(ending, trueRouteApproved)) return false;
            State.ending = ending;
            if (escapes)
            {
                State.world = WorldPhase.Present;
                State.inventory.RemoveAll(x => x.pastOnly);
            }
            return true;
        }

        private static bool AddUnique(List<string> list, string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An ID is required.");
            if (list.Contains(id)) return false;
            list.Add(id); return true;
        }
    }
}

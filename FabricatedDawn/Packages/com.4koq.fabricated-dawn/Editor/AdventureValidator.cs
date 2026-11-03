using System;
using System.Collections.Generic;
using UnityEngine;

namespace FabricatedDawn.Editor
{
    public static class AdventureValidator
    {
        public static List<string> Errors(AdventureDefinition content)
        {
            var errors = new List<string>();
            if (content == null) { errors.Add("Missing adventure asset."); return errors; }
            if (string.IsNullOrWhiteSpace(content.contentId)) errors.Add("Missing content ID.");
            if (content.rooms == null || content.items == null || content.clues == null || content.dialogues == null)
            { errors.Add("Missing content arrays."); return errors; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var puzzleIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var room in content.rooms)
            {
                if (room == null) { errors.Add("Null room."); continue; }
                Id(room.id, ids, errors);
                if (room.hotspots == null) { errors.Add("Null hotspot list: " + room.id); continue; }
                foreach (var h in room.hotspots)
                {
                    if (h == null) { errors.Add("Null hotspot: " + room.id); continue; }
                    if (!string.IsNullOrEmpty(h.solvesPuzzle)) puzzleIds.Add(h.solvesPuzzle);
                }
            }
            foreach (var item in content.items)
            {
                if (item == null) { errors.Add("Null item."); continue; }
                Id(item.id, ids, errors);
                if (item.uses < 1) errors.Add("Invalid item uses: " + item.id);
            }
            foreach (var clue in content.clues)
            {
                if (clue == null) { errors.Add("Null clue."); continue; }
                Id(clue.id, ids, errors);
                if (string.IsNullOrWhiteSpace(clue.title) || string.IsNullOrWhiteSpace(clue.body)) errors.Add("Empty clue: " + clue.id);
            }
            foreach (var dialogue in content.dialogues)
            {
                if (dialogue == null) { errors.Add("Null dialogue."); continue; }
                Id(dialogue.id, ids, errors);
                if (dialogue.lines == null || dialogue.lines.Length == 0 || Array.Exists(dialogue.lines, string.IsNullOrWhiteSpace))
                    errors.Add("Empty dialogue: " + dialogue.id);
            }
            if (content.Room(content.startRoomId) == null) errors.Add("Unknown start room.");
            if (!puzzleIds.Contains(content.completionPuzzleId ?? "")) errors.Add("Completion puzzle is never solved.");
            foreach (var room in content.rooms)
            {
                if (room?.hotspots == null) continue;
                foreach (var h in room.hotspots)
                {
                    if (h == null) continue;
                    Id(h.id, ids, errors);
                    if (string.IsNullOrWhiteSpace(h.label) || string.IsNullOrWhiteSpace(h.response)) errors.Add("Missing label/feedback: " + h.id);
                    if (!Enum.IsDefined(typeof(InteractionKind), h.kind)) errors.Add("Unknown interaction kind: " + h.id);
                    if (!Finite(h.bounds.x) || !Finite(h.bounds.y) || !Finite(h.bounds.width) || !Finite(h.bounds.height) ||
                        h.bounds.width <= 0 || h.bounds.height <= 0 || h.bounds.xMin < 0 || h.bounds.yMin < 0 ||
                        h.bounds.xMax > 1 || h.bounds.yMax > 1) errors.Add("Invalid hotspot bounds: " + h.id);
                    foreach (var puzzle in new[] { h.requiredPuzzle, h.visibleAfterPuzzle, h.hiddenAfterPuzzle })
                        if (!string.IsNullOrEmpty(puzzle) && !puzzleIds.Contains(puzzle)) errors.Add("Puzzle has no producer: " + h.id + " -> " + puzzle);
                    if ((h.kind == InteractionKind.Pickup || h.kind == InteractionKind.UseItem) &&
                        (h.item == null || !Array.Exists(content.items, x => x == h.item))) errors.Add("Unknown item: " + h.id);
                    if (h.kind == InteractionKind.ReadClue && content.Clue(h.clueId) == null) errors.Add("Unknown clue: " + h.id);
                    if (h.kind == InteractionKind.Talk && content.Dialogue(h.dialogueId) == null) errors.Add("Unknown dialogue: " + h.id);
                    if (h.kind == InteractionKind.Travel && content.Room(h.destinationRoom) == null) errors.Add("Unknown destination: " + h.id);
                    if (!string.IsNullOrEmpty(h.unlocksRoom) && content.Room(h.unlocksRoom) == null) errors.Add("Unknown unlock: " + h.id);
                }
            }
            return errors;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static void Id(string id, HashSet<string> ids, List<string> errors)
        { if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) errors.Add("Empty/duplicate ID: " + id); }
    }
}

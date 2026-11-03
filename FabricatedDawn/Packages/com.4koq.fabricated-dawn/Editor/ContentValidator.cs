using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FabricatedDawn.Editor
{
    [Serializable]
    public sealed class ContentReport
    {
        public string unityVersion;
        public int chapterCount;
        public int roomCount;
        public int itemCount;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
    }

    public static class ContentValidator
    {
        public static ContentReport Inspect(ProjectCatalog catalog)
        {
            var report = new ContentReport { unityVersion = Application.unityVersion };
            if (catalog == null) { report.errors.Add("Missing ProjectCatalog."); return report; }
            if (catalog.chapters == null || catalog.rooms == null || catalog.items == null)
            { report.errors.Add("Missing catalog arrays."); return report; }
            report.chapterCount = catalog.chapters.Length; report.roomCount = catalog.rooms.Length;
            report.itemCount = catalog.items.Length;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var rooms = new HashSet<string>(catalog.rooms.Where(r => r != null).Select(r => r.id));
            var itemObjects = new HashSet<ItemDefinition>(catalog.items.Where(i => i != null));
            var chapterNumbers = new HashSet<int>();
            var checkpointIds = new HashSet<string>();
            if (catalog.chapters.Length != 8) report.errors.Add("Expected exactly eight chapters.");
            foreach (var c in catalog.chapters)
            {
                if (c == null) { report.errors.Add("Null chapter reference."); continue; }
                CheckId(c.id, ids, report);
                if (c.number < 1 || c.number > 8 || !chapterNumbers.Add(c.number)) report.errors.Add("Invalid or duplicate chapter number: " + c.id);
                var expected = c.number <= 4 ? WorldPhase.Present : WorldPhase.PastEnclosed;
                if (c.startingWorld != expected) report.errors.Add("Wrong starting world: " + c.id);
                if (string.IsNullOrWhiteSpace(c.completionCriteria)) report.errors.Add("Missing completion criteria: " + c.id);
                if (c.checkpoints == null || c.checkpoints.Length == 0) report.errors.Add("No checkpoint: " + c.id);
                else foreach (var cp in c.checkpoints) CheckId(cp, checkpointIds, report);
            }
            foreach (var room in catalog.rooms)
            {
                if (room == null) { report.errors.Add("Null room reference."); continue; }
                CheckId(room.id, ids, report);
                if (!room.presentBackground || !room.pastBackground) report.warnings.Add("Placeholder backgrounds: " + room.id);
                CheckHotspots(room.presentHotspots, ids, rooms, itemObjects, report);
                CheckHotspots(room.pastHotspots, ids, rooms, itemObjects, report);
            }
            foreach (var item in catalog.items)
            {
                if (item == null) { report.errors.Add("Null item reference."); continue; }
                CheckId(item.id, ids, report);
                if (item.uses < 1) report.errors.Add("Item needs at least one use: " + item.id);
                if (!Enum.IsDefined(typeof(ItemOrigin), item.origin)) report.errors.Add("Invalid item origin: " + item.id);
                if (!item.icon) report.warnings.Add("Placeholder icon: " + item.id);
                if (item.id == "ITEM_MEMORY_KNIFE" && (!item.pastOnly || item.origin != ItemOrigin.Linked))
                    report.errors.Add("Memory knife must be linked and past-only.");
            }
            if (!catalog.items.Any(i => i != null && i.id == "ITEM_MEMORY_KNIFE")) report.errors.Add("Missing memory knife.");
            if (!catalog.trueRouteApproved) report.warnings.Add("True Ending is pending narrative approval; keep route locked.");
            return report;
        }

        private static void CheckHotspots(HotspotDefinition[] hotspots, HashSet<string> ids, HashSet<string> rooms,
            HashSet<ItemDefinition> items, ContentReport report)
        {
            if (hotspots == null) { report.errors.Add("Null hotspot array."); return; }
            foreach (var h in hotspots)
            {
                if (h == null) { report.errors.Add("Null hotspot."); continue; }
                CheckId(h.id, ids, report);
                if (h.bounds.width <= 0 || h.bounds.height <= 0 || h.bounds.xMin < 0 || h.bounds.yMin < 0 ||
                    h.bounds.xMax > 1 || h.bounds.yMax > 1) report.errors.Add("Hotspot bounds outside 0..1: " + h.id);
                if (!string.IsNullOrEmpty(h.destinationRoomId) && !rooms.Contains(h.destinationRoomId))
                    report.errors.Add("Unknown destination: " + h.id);
                if (h.requiredItem != null && !items.Contains(h.requiredItem)) report.errors.Add("Item outside catalog: " + h.id);
                if (string.IsNullOrWhiteSpace(h.response)) report.warnings.Add("Missing interaction feedback: " + h.id);
            }
        }

        private static void CheckId(string id, HashSet<string> ids, ContentReport report)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) report.errors.Add("Empty or duplicate ID: " + id);
        }
    }
}

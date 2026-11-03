using System;
using UnityEngine;

namespace FabricatedDawn
{
    [CreateAssetMenu(menuName = "Fabricated Dawn/Project Catalog")]
    public sealed class ProjectCatalog : ScriptableObject
    {
        public string sourceDocumentUrl;
        [Tooltip("Keep false until the team resolves the friend twist and approves its clue chain.")]
        public bool trueRouteApproved;
        public ChapterDefinition[] chapters = Array.Empty<ChapterDefinition>();
        public RoomDefinition[] rooms = Array.Empty<RoomDefinition>();
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
    }
}

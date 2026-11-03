using System;
using UnityEngine;

namespace FabricatedDawn
{
    [Serializable]
    public sealed class HotspotDefinition
    {
        public string id;
        [Tooltip("Coordinates in the fixed 2D scene, normalized to 0..1.")]
        public Rect bounds = new Rect(.1f, .1f, .15f, .15f);
        public string requiredPuzzleId;
        public ItemDefinition requiredItem;
        public string destinationRoomId;
        public string response;
    }

    [CreateAssetMenu(menuName = "Fabricated Dawn/Room")]
    public sealed class RoomDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite presentBackground;
        public Sprite pastBackground;
        public bool hasOutsideExit;
        public HotspotDefinition[] presentHotspots = Array.Empty<HotspotDefinition>();
        public HotspotDefinition[] pastHotspots = Array.Empty<HotspotDefinition>();
    }
}

using UnityEngine;

namespace FabricatedDawn
{
    [CreateAssetMenu(menuName = "Fabricated Dawn/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public ItemOrigin origin;
        public bool pastOnly;
        [Min(1)] public int uses = 1;
        [TextArea] public string purpose;
        public Sprite icon;
    }
}

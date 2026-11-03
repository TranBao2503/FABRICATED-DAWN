using UnityEngine;

namespace FabricatedDawn
{
    [CreateAssetMenu(menuName = "Fabricated Dawn/Chapter")]
    public sealed class ChapterDefinition : ScriptableObject
    {
        public string id;
        [Range(1, 8)] public int number = 1;
        public string displayName;
        public WorldPhase startingWorld;
        [TextArea(2, 6)] public string objective;
        [TextArea(2, 6)] public string completionCriteria;
        public string[] checkpoints;
    }
}

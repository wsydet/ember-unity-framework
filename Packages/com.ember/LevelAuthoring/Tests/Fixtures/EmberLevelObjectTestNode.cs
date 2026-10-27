using UnityEngine;

namespace Ember.LevelAuthoring.Tests
{
    // 仅用于编辑事务回归，不包含产品类型或运行时玩法。
    public sealed class EmberLevelObjectTestNode : MonoBehaviour
    {
        public string stableId;
        public EmberLevelObjectTestNode link;
        public Vector2 range;
    }
}

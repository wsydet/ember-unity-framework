using System;
using UnityEngine;

namespace Ember.LevelAuthoring.Tests
{
    public sealed class EmberRoomGraphTestAsset : ScriptableObject
    {
        [Serializable] public sealed class Room
        {
            public string id;
            public Vector2 graphPosition, authoredWorldOrigin;
        }
        [Serializable] public sealed class Link { public string from, to; }
        public Room[] rooms = { new() { id = "a", authoredWorldOrigin = new Vector2(3, 7) }, new() { id = "b", authoredWorldOrigin = new Vector2(9, 11) } };
        public Link[] links = Array.Empty<Link>();
    }
}

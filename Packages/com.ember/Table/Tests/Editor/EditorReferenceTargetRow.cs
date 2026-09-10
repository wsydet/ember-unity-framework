// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.Table.Editor.Tests
{
    [EmberTable("reference_target")]
    public sealed class EditorReferenceTargetRow
    {
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }

        [EmberTableConstructor]
        public EditorReferenceTargetRow(string id)
        {
            Id = id;
        }
    }
}

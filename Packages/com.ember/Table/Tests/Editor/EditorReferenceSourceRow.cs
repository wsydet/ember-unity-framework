// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.Table.Editor.Tests
{
    [EmberTable("reference_source")]
    public sealed class EditorReferenceSourceRow
    {
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }

        [EmberTableColumn("targetId"), EmberTableReference("reference_target")]
        public string TargetId { get; }

        [EmberTableConstructor]
        public EditorReferenceSourceRow(string id, string targetId)
        {
            Id = id;
            TargetId = targetId;
        }
    }
}

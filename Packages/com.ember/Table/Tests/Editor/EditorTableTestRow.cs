// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.Table.Editor.Tests
{
    public enum EditorTableTestRank : short
    {
        Low = -1,
        High = 2,
    }

    [EmberTable("editor_test")]
    public sealed class EditorTableTestRow
    {
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }

        [EmberTableColumn("enabled")]
        public bool Enabled { get; }

        [EmberTableColumn("amount")]
        public decimal Amount { get; }

        [EmberTableColumn("rank")]
        public EditorTableTestRank Rank { get; }

        [EmberTableColumn("optional")]
        public int? Optional { get; }

        [EmberTableConstructor]
        public EditorTableTestRow(
            string id,
            bool enabled,
            decimal amount,
            EditorTableTestRank rank,
            int? optional)
        {
            Id = id;
            Enabled = enabled;
            Amount = amount;
            Rank = rank;
            Optional = optional;
        }
    }
}

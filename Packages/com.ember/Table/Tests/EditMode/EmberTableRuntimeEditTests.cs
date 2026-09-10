// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Ember.Table.Tests
{
    public sealed class EmberTableRuntimeEditTests
    {
        private static readonly byte[] SchemaHash = new byte[32];
        private static readonly byte[] SourceHash = Enumerable.Repeat((byte)0x11, 32).ToArray();

        [Test]
        public void Engine_LoadsQueriesAndPreservesSourceOrder()
        {
            var binding = new TestBinding("items");
            byte[] file = BuildFile(binding, ("A", 1), ("a", 2), ("B", 3));
            using var engine = new EmberTableEngine();

            EmberTableLoadResult result = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", file)));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(engine.Database.TryGetTable("items", out EmberTable<TestRow> table), Is.True);
            Assert.That(table.Select(row => row.Id), Is.EqualTo(new[] { "A", "a", "B" }));
            Assert.That(table.TryGet("A", out TestRow upper), Is.True);
            Assert.That(upper.Value, Is.EqualTo(1));
            Assert.That(table.TryGet("a", out TestRow lower), Is.True);
            Assert.That(lower.Value, Is.EqualTo(2));
            Assert.That(table.TryGet("missing", out _), Is.False);
            Assert.That(engine.Database.TryGetTable<TestRow>("unknown", out _), Is.False);
            Assert.That(table, Is.Not.InstanceOf<ICollection<TestRow>>());
        }

        [Test]
        public void DuplicateAndEmptyPrimaryKeysFailDeterministically()
        {
            var binding = new TestBinding("items");
            using var engine = new EmberTableEngine();

            EmberTableLoadResult duplicate = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", BuildFile(binding, ("same", 1), ("same", 2)))));
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.DuplicatePrimaryKey));

            EmberTableLoadResult empty = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", BuildFile(binding, (string.Empty, 1)))));
            Assert.That(empty.Succeeded, Is.False);
            Assert.That(empty.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.EmptyPrimaryKey));
        }

        [Test]
        public void RequiredFailurePreservesOldSnapshot()
        {
            var first = new TestBinding("first");
            var missing = new TestBinding("missing");
            using var engine = new EmberTableEngine();
            Assert.That(engine.Load(
                Catalog(new EmberTableCatalogEntry(first)),
                Sources(("first", BuildFile(first, ("old", 1))))).Succeeded, Is.True);
            EmberTableDatabase oldDatabase = engine.Database;

            EmberTableLoadResult failed = engine.Load(
                Catalog(new EmberTableCatalogEntry(first), new EmberTableCatalogEntry(missing)),
                Sources(("first", BuildFile(first, ("new", 2)))));

            Assert.That(failed.Succeeded, Is.False);
            Assert.That(engine.Database, Is.SameAs(oldDatabase));
            Assert.That(engine.Database.TryGetTable("first", out EmberTable<TestRow> table), Is.True);
            Assert.That(table.ContainsKey("old"), Is.True);
            Assert.That(table.ContainsKey("new"), Is.False);
        }

        [Test]
        public void OptionalFailureIsOmittedFromNewSnapshot()
        {
            var required = new TestBinding("required");
            var optional = new TestBinding("optional");
            using var engine = new EmberTableEngine();
            var catalog = Catalog(
                new EmberTableCatalogEntry(required),
                new EmberTableCatalogEntry(optional, false));
            Assert.That(engine.Load(
                catalog,
                Sources(
                    ("required", BuildFile(required, ("old", 1))),
                    ("optional", BuildFile(optional, ("optional-old", 1))))).Succeeded, Is.True);

            EmberTableLoadResult reloaded = engine.Load(
                catalog,
                Sources(("required", BuildFile(required, ("new", 2)))));

            Assert.That(reloaded.Succeeded, Is.True);
            Assert.That(engine.Database.ContainsTable("required"), Is.True);
            Assert.That(engine.Database.ContainsTable("optional"), Is.False);
            Assert.That(reloaded.Diagnostics.Any(item => item.Code == EmberTableErrorCode.OptionalTableOmitted), Is.True);
        }

        [Test]
        public void ExplicitSecondaryIndexPreservesSourceOrderAndEnforcesUniqueKeys()
        {
            var binding = new TestBinding("items", addValueIndex: true);
            using var engine = new EmberTableEngine();
            EmberTableLoadResult loaded = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", BuildFile(binding, ("A", 1), ("B", 2), ("C", 1)))));

            Assert.That(loaded.Succeeded, Is.True);
            Assert.That(engine.Database.TryGetTable("items", out EmberTable<TestRow> table), Is.True);
            Assert.That(table.TryGetIndex("by_value", out EmberTableIndex<int, TestRow> index), Is.True);
            Assert.That(index.TryGet(1, out IReadOnlyList<TestRow> rows), Is.True);
            Assert.That(rows.Select(row => row.Id), Is.EqualTo(new[] { "A", "C" }));

            var unique = new TestBinding("unique", addValueIndex: true, uniqueValueIndex: true);
            EmberTableLoadResult duplicate = engine.Load(
                Catalog(new EmberTableCatalogEntry(unique)),
                Sources(("unique", BuildFile(unique, ("A", 1), ("B", 1)))));
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.DuplicateSecondaryKey));
        }

        [Test]
        public void DuplicateTableIdFailsBeforeSnapshotConstruction()
        {
            var binding = new TestBinding("items");
            using var engine = new EmberTableEngine();
            EmberTableLoadResult result = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding), new EmberTableCatalogEntry(binding)),
                Sources(("items", BuildFile(binding, ("one", 1)))));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.DuplicateTableId));
        }

        [TestCase(0, EmberTableErrorCode.InvalidMagic)]
        [TestCase(6, EmberTableErrorCode.UnsupportedFlags)]
        public void CodecRejectsInvalidHeader(int offset, EmberTableErrorCode expected)
        {
            var binding = new TestBinding("items");
            byte[] file = BuildFile(binding, ("one", 1));
            file[offset] = 1;
            using var engine = new EmberTableEngine();
            EmberTableLoadResult result = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", file)));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expected));
        }

        [Test]
        public void CodecRejectsPayloadHashAndTrailingData()
        {
            var binding = new TestBinding("items");
            byte[] corrupt = BuildFile(binding, ("one", 1));
            corrupt[^1] ^= 0x7f;
            using var engine = new EmberTableEngine();
            EmberTableLoadResult hash = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", corrupt)));
            Assert.That(hash.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.PayloadHashMismatch));

            var writer = new EmberTableBinaryWriter();
            writer.WriteString("one");
            writer.WriteInt32(1);
            writer.WriteByte(99);
            byte[] trailing = EmberTableBinaryFormat.BuildFile("items", binding.RowTypeId, SchemaHash, SourceHash, 1, writer.ToArray());
            EmberTableLoadResult tail = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", trailing)));
            Assert.That(tail.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.PayloadTrailingData));
        }

        [Test]
        public void CodecRejectsIdentifiersSchemaLengthsAndLimits()
        {
            var binding = new TestBinding("items");
            byte[] valid = BuildFile(binding, ("one", 1));
            using var engine = new EmberTableEngine();

            var wrongTableBinding = new TestBinding("other");
            EmberTableLoadResult tableId = engine.Load(
                Catalog(new EmberTableCatalogEntry(wrongTableBinding)),
                Sources(("other", valid)));
            Assert.That(tableId.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.TableIdMismatch));

            var wrongSchemaBinding = new TestBinding("items", new string('f', 64));
            EmberTableLoadResult schema = engine.Load(
                Catalog(new EmberTableCatalogEntry(wrongSchemaBinding)),
                Sources(("items", valid)));
            Assert.That(schema.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.SchemaHashMismatch));

            EmberTableLoadResult rows = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding, true, 1024, 0, 1024)),
                Sources(("items", valid)));
            Assert.That(rows.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.LimitExceeded));

            byte[] truncated = valid.Take(valid.Length - 1).ToArray();
            EmberTableLoadResult length = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", truncated)));
            Assert.That(length.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.PayloadLengthMismatch));

            byte[] invalidStringPayload = { 0xfe, 0xff, 0xff, 0xff, 1, 0, 0, 0 };
            byte[] invalidString = EmberTableBinaryFormat.BuildFile(
                "items", binding.RowTypeId, SchemaHash, SourceHash, 1, invalidStringPayload);
            EmberTableLoadResult stringLength = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)),
                Sources(("items", invalidString)));
            Assert.That(stringLength.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.InvalidLength));

            var wrongRowBinding = new TestBinding("items", rowTypeId: "Other.Assembly:Other.Row");
            EmberTableLoadResult rowType = engine.Load(
                Catalog(new EmberTableCatalogEntry(wrongRowBinding)),
                Sources(("items", valid)));
            Assert.That(rowType.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.RowTypeIdMismatch));

            EmberTableLoadResult fileLimit = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding, true, valid.Length - 1)),
                Sources(("items", valid)));
            Assert.That(fileLimit.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.LimitExceeded));

            EmberTableLoadResult stringLimit = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding, true, 1024, 10, 2)),
                Sources(("items", valid)));
            Assert.That(stringLimit.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.LimitExceeded));
        }

        [Test]
        public void CodecRejectsUnsupportedVersionNegativeCountsAndInvalidUtf8()
        {
            var binding = new TestBinding("items");
            byte[] valid = BuildFile(binding, ("one", 1));
            using var engine = new EmberTableEngine();

            byte[] version = (byte[])valid.Clone();
            version[4] = 2;
            EmberTableLoadResult unsupported = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)), Sources(("items", version)));
            Assert.That(unsupported.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.UnsupportedFormatVersion));

            int rowCountOffset = FindRowCountOffset(valid);
            byte[] negativeRows = (byte[])valid.Clone();
            WriteInt32(negativeRows, rowCountOffset, -1);
            EmberTableLoadResult rows = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)), Sources(("items", negativeRows)));
            Assert.That(rows.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.InvalidLength));

            byte[] negativePayload = (byte[])valid.Clone();
            WriteInt32(negativePayload, rowCountOffset + 4, -1);
            EmberTableLoadResult payload = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)), Sources(("items", negativePayload)));
            Assert.That(payload.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.InvalidLength));

            byte[] invalidUtf8Payload = { 1, 0, 0, 0, 0xff, 1, 0, 0, 0 };
            byte[] invalidUtf8 = EmberTableBinaryFormat.BuildFile(
                binding.TableId,
                binding.RowTypeId,
                SchemaHash,
                SourceHash,
                1,
                invalidUtf8Payload);
            EmberTableLoadResult utf8 = engine.Load(
                Catalog(new EmberTableCatalogEntry(binding)), Sources(("items", invalidUtf8)));
            Assert.That(utf8.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.InvalidUtf8));

            var boolError = Assert.Throws<EmberTableDataException>(
                () => new EmberTableBinaryReader(new byte[] { 2 }).ReadBoolean());
            Assert.That(boolError.Code, Is.EqualTo(EmberTableErrorCode.InvalidBoolean));
            var nullableError = Assert.Throws<EmberTableDataException>(
                () => new EmberTableBinaryReader(new byte[] { 2 }).ReadNullableMarker());
            Assert.That(nullableError.Code, Is.EqualTo(EmberTableErrorCode.InvalidNullableMarker));
        }

        [Test]
        public void BinaryReaderWriterFreezeNumericDecimalNullableAndUtf8WireFormat()
        {
            var writer = new EmberTableBinaryWriter();
            writer.WriteBoolean(true);
            writer.WriteSByte(-2);
            writer.WriteByte(0xfe);
            writer.WriteInt16(-300);
            writer.WriteUInt16(60000);
            writer.WriteInt32(-1234567);
            writer.WriteUInt32(4000000000);
            writer.WriteInt64(-9000000000000);
            writer.WriteUInt64(18000000000000);
            writer.WriteSingle(1.5f);
            writer.WriteDouble(-2.25);
            writer.WriteDecimal(123.45m);
            writer.WriteNullableMarker(false);
            writer.WriteString("表");

            var reader = new EmberTableBinaryReader(writer.ToArray());
            Assert.That(reader.ReadBoolean(), Is.True);
            Assert.That(reader.ReadSByte(), Is.EqualTo(-2));
            Assert.That(reader.ReadByte(), Is.EqualTo(0xfe));
            Assert.That(reader.ReadInt16(), Is.EqualTo(-300));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(60000));
            Assert.That(reader.ReadInt32(), Is.EqualTo(-1234567));
            Assert.That(reader.ReadUInt32(), Is.EqualTo(4000000000));
            Assert.That(reader.ReadInt64(), Is.EqualTo(-9000000000000));
            Assert.That(reader.ReadUInt64(), Is.EqualTo(18000000000000));
            Assert.That(reader.ReadSingle(), Is.EqualTo(1.5f));
            Assert.That(reader.ReadDouble(), Is.EqualTo(-2.25));
            Assert.That(reader.ReadDecimal(), Is.EqualTo(123.45m));
            Assert.That(reader.ReadNullableMarker(), Is.False);
            Assert.That(reader.ReadString(), Is.EqualTo("表"));
            Assert.That(reader.IsFullyConsumed, Is.True);
        }

        [Test]
        public void EmptyV1FileMatchesFrozenCrossPlatformVector()
        {
            byte[] file = EmberTableBinaryFormat.BuildFile(
                "t",
                "A:R",
                new byte[32],
                SourceHash,
                0,
                Array.Empty<byte>());

            const string expected =
                "4554424c010000000100740300413a52000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111111111111111111111111111111111111111110000000000000000e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            Assert.That(EmberTableBinaryFormat.ToHex(file), Is.EqualTo(expected));
            Assert.That(EmberTableBinaryFormat.ToHex(EmberTableBinaryFormat.ComputeSha256(Array.Empty<byte>())),
                Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        }

        [Test]
        public void DisposeIsIdempotentAndPreventsReload()
        {
            var engine = new EmberTableEngine();
            engine.Dispose();
            engine.Dispose();
            Assert.That(engine.Database.Count, Is.Zero);
            EmberTableLoadResult result = engine.Load(EmberTableCatalog.Empty, Sources());
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(EmberTableErrorCode.EngineDisposed));
        }

        private static EmberTableCatalog Catalog(params EmberTableCatalogEntry[] entries)
        {
            return new EmberTableCatalog(entries);
        }

        private static Dictionary<string, byte[]> Sources(params (string id, byte[] bytes)[] values)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++) result.Add(values[i].id, values[i].bytes);
            return result;
        }

        private static byte[] BuildFile(TestBinding binding, params (string id, int value)[] rows)
        {
            var writer = new EmberTableBinaryWriter();
            for (int i = 0; i < rows.Length; i++)
            {
                writer.WriteString(rows[i].id);
                writer.WriteInt32(rows[i].value);
            }
            return EmberTableBinaryFormat.BuildFile(
                binding.TableId,
                binding.RowTypeId,
                SchemaHash,
                SourceHash,
                rows.Length,
                writer.ToArray());
        }

        private static int FindRowCountOffset(byte[] file)
        {
            int tableIdLength = file[8] | file[9] << 8;
            int rowTypeLengthOffset = 10 + tableIdLength;
            int rowTypeLength = file[rowTypeLengthOffset] | file[rowTypeLengthOffset + 1] << 8;
            return rowTypeLengthOffset + 2 + rowTypeLength + 64;
        }

        private static void WriteInt32(byte[] bytes, int offset, int value)
        {
            uint raw = unchecked((uint)value);
            bytes[offset] = (byte)raw;
            bytes[offset + 1] = (byte)(raw >> 8);
            bytes[offset + 2] = (byte)(raw >> 16);
            bytes[offset + 3] = (byte)(raw >> 24);
        }

        private sealed class TestBinding : EmberTableBinding<TestRow>
        {
            private readonly bool _addValueIndex;
            private readonly bool _uniqueValueIndex;

            public TestBinding(
                string tableId,
                string schemaHash = null,
                bool addValueIndex = false,
                bool uniqueValueIndex = false,
                string rowTypeId = "Ember.Table.Tests:TestRow")
                : base(
                    tableId,
                    rowTypeId,
                    "Config/Tables/" + tableId,
                    schemaHash ?? new string('0', 64))
            {
                _addValueIndex = addValueIndex;
                _uniqueValueIndex = uniqueValueIndex;
            }

            protected override TestRow ReadRow(EmberTableBinaryReader reader)
            {
                return new TestRow(reader.ReadString(), reader.ReadInt32());
            }

            protected override string GetPrimaryKey(TestRow row)
            {
                return row.Id;
            }

            protected override void BuildSecondaryIndexes(EmberTable<TestRow> table)
            {
                if (_addValueIndex)
                    AddSecondaryIndex(table, "by_value", row => row.Value, _uniqueValueIndex);
            }
        }

        private sealed class TestRow
        {
            public string Id { get; }
            public int Value { get; }

            public TestRow(string id, int value)
            {
                Id = id;
                Value = value;
            }
        }
    }
}

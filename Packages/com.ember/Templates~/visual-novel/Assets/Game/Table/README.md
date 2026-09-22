# Project tables

1. Put immutable Row types in `Rows` and mark them with `EmberTable`, `EmberTableKey`, column metadata and one `EmberTableConstructor`.
2. Put `.etable.csv` or `.etable.tsv` sources in `Assets/GameResource/TableSources`.
3. Create `EmberTableDefinition` assets in `Definitions`, then use `Ember/配置表中心` to validate, preview and bake all tables.
4. Enable `GameTableModule` only after the project needs tables. Gameplay code checks `IsReady` and normally consumes a project-owned domain adapter.

Minimal Row shape:

```csharp
[EmberTable("sample")]
public sealed class SampleRow
{
    [EmberTableKey, EmberTableColumn("id")]
    public string Id { get; }

    [EmberTableConstructor]
    public SampleRow(string id) { Id = id; }
}
```

The generated Catalog/Binding files and `.bytes` files are generator-owned. Do not edit them by hand.

// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUIDynamicSummaryItem
    {
        public void SetCount(int count) => Label.SetSource(count == 0 ? "Dynamic collection  /  empty" : "Dynamic collection  /  " + count + " items");
        public void SetSelected(string key, int index) => Label.SetSource("Selected: " + key + "  /  position " + (index + 1));
    }
}

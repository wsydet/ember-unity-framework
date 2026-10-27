// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Game.UI
{
    public partial class EUIGMStateItem
    {
        public void SetState(string stateName)
        {
            string value = string.IsNullOrEmpty(stateName) ? "—" : stateName;
            if (Label.text != value) Label.SetSource(value);
        }
    }
}

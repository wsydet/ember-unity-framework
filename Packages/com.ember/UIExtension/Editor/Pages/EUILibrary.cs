// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>可分发的公共库维护源；目录 JSON 只作为生成输出。</summary>
    public sealed class EUILibrary : ScriptableObject
    {
        #region 编辑器面板参数
        public string SkinId = "EmberDefaultLight";
        public string SkinVersion = "1.0.0";
        public Vector2 ReferenceResolution = new Vector2(1280, 720);
        public List<EUILibraryEntry> Entries = new();
        #endregion
    }

    [Serializable]
    public sealed class EUILibraryEntry
    {
        public string Id, Family, Category;
        public GameObject Prefab;
        public bool AutoMatch;
        public string[] Aliases = Array.Empty<string>();
        public string[] RequiredRoles = Array.Empty<string>();
        public string[] AllowedOverrides = { "root.position", "root.size" };
        public Vector2 ReferenceSize;
        public List<EUILibrarySlot> Slots = new();
    }

    [Serializable]
    public sealed class EUILibrarySlot
    {
        public string Id, Role, ComponentType, State;
        public Component Target;
        public bool Required = true;
        public string[] Properties = Array.Empty<string>();
    }

    [Serializable]
    public sealed class EUILibraryDescription
    {
        public string id, family, category, prefabGuid, skinId, skinVersion;
        public string[] allowedOverrides;
        public List<EUILibrarySlotDescription> slots = new();
    }

    [Serializable]
    public sealed class EUILibrarySlotDescription
    {
        public string id, role, targetId, path, componentType, state;
        public string[] properties;
    }

    [Serializable]
    public sealed class EUILibraryCatalog
    {
        public int schemaVersion = 1;
        public string inputDigest;
        public List<EUILibraryDescription> entries = new();
    }
}

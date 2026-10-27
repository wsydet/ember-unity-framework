// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;

namespace Ember.UIExtension.Editor
{
    [Serializable] public sealed class EmberBuiRequest
    {
        public int schemaVersion = 1;
        public string mapPath, ownerTemplateId, moduleType, receiptPath;
        public string fontPolicy = "preserve-existing";
        public string newTextFontPath;
        public List<EmberBuiTargetEdit> targets = new();
    }

    [Serializable] public sealed class EmberBuiTargetEdit
    {
        public string targetId;
        public string mode = "update";
        public string prefabPath, prefabGuid, sourceSkeletonPath, className, classPath, contentPath;
        public bool structureOnly, inheritBinding, applyReferenceResolution;
        public List<EmberBuiNodeEdit> nodes = new();
    }

    /// <summary>Explicit engineering mapping; no names or business expressions are inferred from artwork.</summary>
    [Serializable] public sealed class EmberBuiNodeEdit
    {
        public string nodeId, path, expectedObjectId, parentPath, reason;
        public string operation = "update"; // update/create/reference/preserve
        public bool geometry, appearance = true, active;
        public string imagePath, replaceSourceGuid, geometrySourceNodeId;
        public string copyLayoutPath;
        public UnityEngine.Vector2 textBoxSize;
        public int siblingIndex = -1;
        public string textPolicy = "preserve"; // preserve/design (new static text only)
        public string stateTogglePath, stateSlot;
        public string bindingName, bindingType;
    }

    [Serializable] public sealed class EmberBuiFileState
    {
        public string path, sha256;
    }

    [Serializable] public sealed class EmberBuiPlan
    {
        public string consumerVersion = EmberBuiService.VERSION;
        public string requestPath, requestDigest, projectRoot, digest;
        public bool valid, noChanges;
        public List<string> issues = new(), operations = new(), limitations = new();
        public List<EmberBuiFileState> inputs = new();
        public List<string> writes = new();
    }

    [Serializable] public sealed class EmberBuiReceipt
    {
        public string consumerVersion = EmberBuiService.VERSION;
        public string requestPath, requestDigest, planDigest, status, error, backupPath;
        public List<string> saved = new(), generated = new();
        public List<EmberBuiFileState> after = new();
        public List<string> verification = new();
    }
}

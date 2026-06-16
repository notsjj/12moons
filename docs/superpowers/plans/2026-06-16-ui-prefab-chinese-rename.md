# UI Prefab Chinese Rename Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename existing UI prefab assets, prefab roots, and runtime UI paths from English names to Chinese names.

**Architecture:** Keep the existing `Resources.Load` architecture and update resource paths to the new Chinese prefab names. Preserve prefab GUIDs by moving both `.prefab` and `.prefab.meta` files. Keep all UI layout and serialized component bindings unchanged.

**Tech Stack:** Unity, C#, YAML prefab assets, PowerShell file moves.

---

### Task 1: Rename UI Prefab Assets And Roots

**Files:**
- Move: `My project/Assets/Resources/Prefabs/UI/*.prefab`
- Move: `My project/Assets/Resources/Prefabs/UI/*.prefab.meta`
- Modify: each moved prefab root `m_Name`

- [ ] Move each English prefab file and its `.meta` to the mapped Chinese filename.
- [ ] Change only the root GameObject `m_Name` in each moved prefab to the mapped Chinese name.
- [ ] Do not change RectTransform, anchors, component bindings, images, fonts, or hierarchy layout.

### Task 2: Update Runtime Resource Paths

**Files:**
- Modify: `My project/Assets/Scripts/UI/BaseSceneUIBootstrap.cs`

- [ ] Replace every `new UIType("Prefabs/UI/<EnglishName>", ...)` with the corresponding Chinese `Resources` path.
- [ ] Keep `UIManager` and `UIType` behavior unchanged because `UIType.Name` already derives the runtime instance name from the final path segment.

### Task 3: Update Editor Validation And Smoke Tests

**Files:**
- Modify: `My project/Assets/Editor/BaseSceneUIFrameworkValidator.cs`
- Modify: `My project/Assets/Editor/Runtime/BaseSceneUIFrameworkSmokeTest.cs`
- Modify: `My project/Assets/Editor/Runtime/DeskPanelAtmosphereSmokeTest.cs`
- Modify: `My project/Assets/Editor/Runtime/DocumentSmokeTest.cs`
- Modify: `My project/Assets/Editor/Runtime/LoadingPanelSmokeTest.cs`
- Modify: `My project/Assets/Editor/Runtime/SuspicionPointerSmokeTest.cs`
- Modify: `My project/Assets/Editor/UiArtChinesePrefabStyler.cs`

- [ ] Replace `Assets/Resources/Prefabs/UI/<EnglishName>.prefab` with Chinese asset paths.
- [ ] Replace smoke-test `UIType` examples so they validate Chinese path normalization and Chinese names.
- [ ] Update style-tool filename checks from English `.prefab` names to Chinese `.prefab` names.

### Task 4: Verify References

**Files:**
- Inspect: all project text files under `My project/Assets`

- [ ] Run `rg` for old `Assets/Resources/Prefabs/UI/<EnglishName>.prefab` paths and old runtime `Prefabs/UI/<EnglishName>` paths.
- [ ] Run `rg --files` to confirm Chinese prefab files exist and old English prefab files no longer exist in `Resources/Prefabs/UI`.
- [ ] Run a compile-oriented verification command where available.

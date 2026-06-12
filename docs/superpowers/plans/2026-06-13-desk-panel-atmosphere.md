# DeskPanel Atmosphere Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an adjustable rectangular inner-screen vignette and a white breathing candle glow to the existing DeskPanel without disturbing its layout.

**Architecture:** A focused `DeskPanelAtmosphereView` owns runtime material instances and DOTween breathing animation. A single UI shader renders either vignette or radial glow, while an Only builder performs the narrowly scoped Prefab additions and bindings.

**Tech Stack:** Unity 6, UGUI Image, ShaderLab/HLSL, DOTween, Unity Editor PrefabUtility

---

### Task 1: Define atmosphere Prefab requirements

**Files:**
- Create: `My project/Assets/Editor/Runtime/DeskPanelAtmosphereSmokeTest.cs`

- [ ] Add assertions for `DeskPanelAtmosphereView`, full-screen vignette, candle glow, raycast settings, and glow-before-candle sibling order.
- [ ] Build the editor project and confirm failure because the atmosphere component does not exist.

### Task 2: Implement the shader and runtime controller

**Files:**
- Create: `My project/Assets/Shaders/UI/DeskPanelAtmosphere.shader`
- Create: `My project/Assets/Scripts/UI/DeskPanelAtmosphereView.cs`

- [ ] Implement shader modes for vignette and radial glow.
- [ ] Implement Chinese-described Inspector parameters, material instances, and DOTween glow breathing.
- [ ] Build the editor project and confirm compilation passes.

### Task 3: Add a local-only DeskPanel builder

**Files:**
- Create: `My project/Assets/Editor/DeskPanelAtmosphereOnlyBuilder.cs`
- Modify: `My project/Assets/Resources/Prefabs/UI/DeskPanel.prefab`

- [ ] Implement a menu command that only updates the DeskPanel atmosphere objects and bindings.
- [ ] Run the menu command once to update the existing Prefab without rebuilding other UI.
- [ ] Run the atmosphere Smoke Test.

### Task 4: Final verification

**Files:**
- Verify only

- [ ] Build `Assembly-CSharp-Editor.csproj`.
- [ ] Run `git diff --check` for the scoped files.
- [ ] Confirm the Prefab contains no unrelated RectTransform or hierarchy changes.
- [ ] Verify in Play mode that the vignette does not block clicks and the candle glow breathes softly.

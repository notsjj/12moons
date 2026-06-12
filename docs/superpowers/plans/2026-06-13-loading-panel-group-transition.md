# LoadingPanel Group Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a DOTween-driven grouped LoadingPanel transition with reversed exit order and a runtime `P` key preview.

**Architecture:** `LoadingPanelTransitionView` owns layer discovery, hierarchy-order grouping, position calculation, DOTween playback, and editor sampling. `BaseSceneUIBootstrap` remains the always-active input owner that shows the normally inactive LoadingPanel before requesting a debug playback.

**Tech Stack:** Unity 6, C#, UGUI RectTransform/Image, DOTween, Unity Editor smoke tests

---

### Task 1: Define grouped transition behavior in the smoke test

**Files:**
- Modify: `My project/Assets/Editor/Runtime/LoadingPanelSmokeTest.cs`

- [ ] Add assertions for resolved group count, first-enter/last-exit order, and one-second hold duration.
- [ ] Run the LoadingPanel smoke test and confirm it fails because the grouped-order inspection API does not exist yet.

### Task 2: Rewrite LoadingPanel transition with DOTween

**Files:**
- Modify: `My project/Assets/Scripts/UI/LoadingPanelTransitionView.cs`
- Modify: `My project/Assets/Scripts/UI/Editor/LoadingPanelTransitionViewEditor.cs`

- [ ] Replace manual coroutine playback with a DOTween Sequence while preserving the public transition API.
- [ ] Pair left and right direct children by hierarchy order and make unmatched layers single groups.
- [ ] Insert groups in forward order for entry and reverse order for original-direction exit.
- [ ] Keep editor preview sampling aligned with the runtime group timing.
- [ ] Replace corrupted Inspector labels with clear Chinese Header and Tooltip text.
- [ ] Run the LoadingPanel smoke test and confirm grouped transition assertions pass.

### Task 3: Add the runtime P-key preview

**Files:**
- Modify: `My project/Assets/Scripts/UI/BaseSceneUIBootstrap.cs`
- Modify: `My project/Assets/Editor/Runtime/LoadingPanelSmokeTest.cs`

- [ ] Add a Chinese-described debug toggle and listen for `P` from the always-active bootstrap.
- [ ] Show LoadingPanel, play debug transition without switching city, and hide it on completion.
- [ ] Add a smoke-test assertion that the bootstrap exposes the P-key debug capability.
- [ ] Run the smoke test and compile the Unity project.

### Task 4: Final verification

**Files:**
- Verify only

- [ ] Run the LoadingPanel smoke test.
- [ ] Run a Unity batch-mode compile or equivalent project compilation check.
- [ ] Inspect the final diff and confirm no unrelated UI, Prefab, or RectTransform changes were introduced.


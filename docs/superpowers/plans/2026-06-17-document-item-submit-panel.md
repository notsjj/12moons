# Document Item Submit Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the five configured item cards exist in the initial backpack and let the inventory panel pop up only while a document requires item submission, with a DOTween submit-slot animation.

**Architecture:** Keep the change local to existing inventory and document UI components. Runtime data owns initial item counts, `DocumentPopupPanelView` decides when item submission is needed, `InventoryPanelView` owns the bottom pop-up/card bounce, and `DocumentSubmitSlot` owns the submitted-card snap animation.

**Tech Stack:** Unity C#, TextMeshPro, Unity UI EventSystem, DOTween, existing Editor smoke-test menu pattern.

---

### Task 1: Red Tests

**Files:**
- Modify: `My project/Assets/Editor/Runtime/InventorySmokeTest.cs`
- Modify: `My project/Assets/Editor/Runtime/DocumentSmokeTest.cs`

- [ ] Add checks that a new game starts with `item_money`, `item_material`, `item_food`, `item_drainage_map`, and `item_archivist_badge` all present with positive counts.
- [ ] Add reflection checks for `InventoryPanelView.ShowForDocumentSubmission`, `InventoryPanelView.HideForDocumentSubmission`, `DocumentSubmitSlot.SubmittedCardPreviewObject`, and `DocumentPopupPanelView.InventoryPanelObject`.
- [ ] Run the smoke tests and confirm they fail because the new initial counts/API are not implemented yet.

### Task 2: Initial Backpack Data

**Files:**
- Modify: `My project/Assets/Scripts/Core/Runtime/RuntimeDataService.cs`

- [ ] Add Inspector fields with Chinese Header/Tooltip for the initial item ids and counts.
- [ ] During `CreateNewGame`, after creating configured item states, apply the initial counts without hard-coding UI content.
- [ ] Keep counts clamped to zero or above.
- [ ] Run the inventory smoke test and confirm the initial backpack checks pass.

### Task 3: Inventory Panel Pop-Up

**Files:**
- Modify: `My project/Assets/Scripts/UI/InventoryPanelView.cs`

- [ ] Add Chinese-documented serialized animation settings for hidden offset, pop duration, return duration, card overshoot, and visibility snapshot.
- [ ] Add public methods `ShowForDocumentSubmission()` and `HideForDocumentSubmission()`.
- [ ] Use DOTween to move the panel from below to its open position, and bounce visible cards upward before returning them inside the content area.
- [ ] Preserve the existing manual layout and keep all card coordinates constrained within the content rect.

### Task 4: Document Submit Animation

**Files:**
- Modify: `My project/Assets/Scripts/UI/DocumentSubmitSlot.cs`
- Modify: `My project/Assets/Scripts/UI/DocumentPopupPanelView.cs`

- [ ] Add references for left scroll end and content viewport, with Chinese Header/Tooltip.
- [ ] Expose `SubmittedCardPreviewObject` for smoke-test inspection.
- [ ] On a correct drop, instantiate/reuse the preview card, place it between content viewport and left scroll end, snap it to the left scroll right boundary, then tween it inside until its left edge aligns with the left scroll left edge.
- [ ] Have `DocumentPopupPanelView` bind and call `InventoryPanelView.ShowForDocumentSubmission()` when the current document requires a submitted item, and `HideForDocumentSubmission()` otherwise.

### Task 5: Local Binding Tool

**Files:**
- Create: `My project/Assets/Editor/DocumentItemSubmitBindingOnlyBuilder.cs`

- [ ] Add a menu item named `Twelve Moons/Setup/Update Document Item Submit Binding Only`.
- [ ] Locate existing `DocumentPopupPanelView`, `InventoryPanelView`, `SubmitCardSlot`, `左滚轴`, and `内容视口`.
- [ ] Set only the new serialized references and do not create, delete, resize, or reposition unrelated UI.
- [ ] Fail with a clear message if the expected existing objects are missing.

### Task 6: Verification

**Files:**
- Modify only as required by compile errors directly related to this task.

- [ ] Run the inventory and document smoke tests.
- [ ] Run the available all-smoke-test entry if practical.
- [ ] Inspect diffs and verify no unrelated layout rebuilds or broad prefab regeneration were introduced.

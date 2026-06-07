# Base Scene UIFramework Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor every `Main Canvas` UI in `BaseScene` into UIFramework-managed Resources prefabs while preserving current layout, bindings, and gameplay UI behavior.

**Architecture:** Extend the existing `UIType + UIManager + PanelManager + BasePanel` framework with four UI layers: Persistent, Panel, Popup, and Overlay. Add a Base Scene UI context/bootstrap layer that owns scene-service references and creates `SharedHudPanel`, `DeskPanel`, `StoryPanel`, `CityHudPanel`, and popup prefabs from `Assets/Resources/Prefabs/UI`.

**Tech Stack:** Unity 6000.2, C#, UGUI, TextMeshPro, DOTween, Unity Editor PrefabUtility, Unity Test Framework, Resources prefabs.

---

## Source Documents

- Spec: `docs/superpowers/specs/2026-06-07-base-scene-ui-framework-refactor-design.md`
- Table plan: `docs/12moons.xlsx`
- Scene: `My project/Assets/Scenes/BaseScene.unity`

## File Map

- Modify: `My project/Assets/Scripts/UIFramework/UIType.cs`
  - Add UI layer metadata and Resources path helpers.
- Modify: `My project/Assets/Scripts/UIFramework/Manager/UIManager.cs`
  - Manage `PersistentRoot`, `PanelRoot`, `PopupRoot`, `OverlayRoot`, instance reuse, lookup, show, hide.
- Modify: `My project/Assets/Scripts/UIFramework/Manager/PanelManager.cs`
  - Keep stack API compatible while using `UIManager` panel methods.
- Modify: `My project/Assets/Scripts/UIFramework/UI/BasePanel.cs`
  - Keep current lifecycle, make closing null-safe, preserve `Push/Pop/PopAll`.
- Create: `My project/Assets/Scripts/UIFramework/UILayer.cs`
  - Enum for UI layer selection.
- Create: `My project/Assets/Scripts/UIFramework/UIHandle.cs`
  - Small runtime wrapper around created UI instance and layer.
- Create: `My project/Assets/Scripts/UI/BaseSceneUIContext.cs`
  - Scene service references with Chinese Header/Tooltip.
- Create: `My project/Assets/Scripts/UI/BaseSceneUIBootstrap.cs`
  - Creates initial UI and bridges existing UI flow.
- Create: `My project/Assets/Scripts/UI/BaseSceneUIPanelRoot.cs`
  - Per-prefab root helper for context injection and debug visibility.
- Create: `My project/Assets/Editor/BaseSceneUIFrameworkPrefabBuilder.cs`
  - Local-only builder for extracting current scene UI into Resources prefabs.
- Create: `My project/Assets/Editor/BaseSceneUIFrameworkValidator.cs`
  - Validates prefabs, scene residue, TMP text heights, missing references.
- Create: `My project/Assets/Editor/Runtime/BaseSceneUIFrameworkSmokeTest.cs`
  - Menu smoke test for framework paths and expected prefab components.
- Modify: `My project/Assets/Scenes/BaseScene.unity`
  - Leave `Main Canvas`, layer roots, `UI Manager`, and non-UI runtime objects; remove migrated concrete UI instances after prefabs are saved.
- Create or update prefabs under: `My project/Assets/Resources/Prefabs/UI`
  - `DeskPanel.prefab`
  - `SharedHudPanel.prefab`
  - `StoryPanel.prefab`
  - `CityHudPanel.prefab`
  - `DocumentPopupPanel.prefab`
  - `NewspaperPanel.prefab`
  - `LetterReaderPanel.prefab`

## Global Constraints

- Do not revert existing user changes, including resource moves into `Assets/Resources`.
- Do not run old total desktop rebuilders such as `DeskUiBuilder`.
- Use only local UIFramework migration builders created in this plan.
- Do not create UI for later stages beyond the UI already present in `BaseScene`.
- Keep all Unity UI text as TMP.
- Ensure all TMP text RectTransform heights are zero or positive.
- Every Inspector-visible field added by this work must have Chinese Header and Chinese Tooltip.

---

### Task 1: Establish Baseline and Package State

**Files:**
- Read: `My project/Packages/manifest.json`
- Read: `My project/Packages/packages-lock.json`
- Read: `My project/Library/Bee/tundra.log.json`

- [ ] **Step 1: Record current compile errors**

Run:

```powershell
rg -n "error CS" "My project\Library\Bee" "My project\Logs"
```

Expected: current errors are recorded before code changes. If `Newtonsoft` is still missing, keep it listed as a pre-existing package resolution blocker.

- [ ] **Step 2: Verify package declaration**

Run:

```powershell
Get-Content "My project\Packages\manifest.json" | Select-String "com.unity.nuget.newtonsoft-json"
Get-Content "My project\Packages\packages-lock.json" | Select-String "com.unity.nuget.newtonsoft-json"
```

Expected: both files mention `com.unity.nuget.newtonsoft-json`.

- [ ] **Step 3: Verify actual PackageCache state**

Run:

```powershell
Get-ChildItem -Force "My project\Library\PackageCache" | Where-Object { $_.Name -match "newtonsoft" } | Select-Object Name
```

Expected: if this returns no rows, Unity has not restored the package into the project cache. The implementation may continue with UI code, but final compile verification must wait until Unity Package Manager resolves the package.

- [ ] **Step 4: Commit nothing**

Do not commit baseline checks. This task records evidence only.

---

### Task 2: Add UIFramework Layer Tests

**Files:**
- Create: `My project/Assets/Editor/Runtime/BaseSceneUIFrameworkSmokeTest.cs`

- [ ] **Step 1: Write the failing smoke test**

Create `BaseSceneUIFrameworkSmokeTest.cs` with this content:

```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class BaseSceneUIFrameworkSmokeTest
    {
        private static readonly string[] RequiredPrefabPaths =
        {
            "Assets/Resources/Prefabs/UI/DeskPanel.prefab",
            "Assets/Resources/Prefabs/UI/SharedHudPanel.prefab",
            "Assets/Resources/Prefabs/UI/StoryPanel.prefab",
            "Assets/Resources/Prefabs/UI/CityHudPanel.prefab",
            "Assets/Resources/Prefabs/UI/DocumentPopupPanel.prefab",
            "Assets/Resources/Prefabs/UI/NewspaperPanel.prefab",
            "Assets/Resources/Prefabs/UI/LetterReaderPanel.prefab"
        };

        [MenuItem("Twelve Moons/Tests/Run Base Scene UIFramework Smoke Test")]
        public static void Run()
        {
            foreach (var path in RequiredPrefabPaths)
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"缺少 UIFramework Prefab：{path}", path);
                }
            }

            var deskPanelType = new UIType("Prefabs/UI/DeskPanel", UILayer.Panel);
            if (deskPanelType.Name != "DeskPanel")
            {
                throw new InvalidOperationException("UIType 未正确解析 UI 名称。");
            }

            if (deskPanelType.Layer != UILayer.Panel)
            {
                throw new InvalidOperationException("UIType 未正确保存 UI 层级。");
            }

            Debug.Log("Base Scene UIFramework smoke test passed.");
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run from Unity menu:

```text
Twelve Moons/Tests/Run Base Scene UIFramework Smoke Test
```

Expected: fails because `UILayer` does not exist yet or target prefabs are not all present.

- [ ] **Step 3: Keep the failing test**

Do not weaken the test. Later tasks make it pass by adding `UILayer`, creating prefabs, and updating `UIType`.

---

### Task 3: Extend UIType and Add UILayer

**Files:**
- Create: `My project/Assets/Scripts/UIFramework/UILayer.cs`
- Modify: `My project/Assets/Scripts/UIFramework/UIType.cs`

- [ ] **Step 1: Add UILayer**

Create `UILayer.cs`:

```csharp
public enum UILayer
{
    Persistent,
    Panel,
    Popup,
    Overlay
}
```

- [ ] **Step 2: Replace UIType implementation**

Update `UIType.cs`:

```csharp
using System;

/// <summary>
/// 存储单个 UI 的 Resources 路径、名称和显示层级。
/// </summary>
public sealed class UIType : IEquatable<UIType>
{
    public string Name { get; private set; }

    public string Path { get; private set; }

    public UILayer Layer { get; private set; }

    public UIType(string path, UILayer layer = UILayer.Panel)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("UI Resources 路径不能为空。", nameof(path));
        }

        Path = path.Replace("\\", "/");
        Layer = layer;
        Name = Path.Substring(Path.LastIndexOf('/') + 1);
    }

    public bool Equals(UIType other)
    {
        return other != null && Path == other.Path && Layer == other.Layer;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as UIType);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ (int)Layer;
        }
    }
}
```

- [ ] **Step 3: Run compile check**

Run:

```powershell
rg -n "new UIType" "My project\Assets\Scripts"
```

Expected: existing `new UIType(path)` call sites still compile because the layer argument has a default.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scripts/UIFramework/UILayer.cs" "My project/Assets/Scripts/UIFramework/UIType.cs" "My project/Assets/Editor/Runtime/BaseSceneUIFrameworkSmokeTest.cs"
git commit -m "feat: add UIFramework layer metadata"
```

---

### Task 4: Add UI Instance Handle and Layered UIManager

**Files:**
- Create: `My project/Assets/Scripts/UIFramework/UIHandle.cs`
- Modify: `My project/Assets/Scripts/UIFramework/Manager/UIManager.cs`

- [ ] **Step 1: Create UIHandle**

Create `UIHandle.cs`:

```csharp
using UnityEngine;

public sealed class UIHandle
{
    public UIHandle(UIType uiType, GameObject gameObject)
    {
        UIType = uiType;
        GameObject = gameObject;
    }

    public UIType UIType { get; }

    public GameObject GameObject { get; }

    public T GetComponent<T>() where T : Component
    {
        return GameObject == null ? null : GameObject.GetComponent<T>();
    }
}
```

- [ ] **Step 2: Replace UIManager with layered implementation**

Update `UIManager.cs` so it:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按 UI 层级创建、复用、查询和销毁 Resources UI Prefab。
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private readonly Dictionary<UIType, UIHandle> uiInstances = new Dictionary<UIType, UIHandle>();
    private readonly Dictionary<UILayer, Transform> layerRoots = new Dictionary<UILayer, Transform>();

    [Header("UI 根画布")]
    [Tooltip("UIFramework 实例化 UI 时使用的主画布；为空时自动查找名为 Main Canvas 的对象。")]
    [SerializeField] private Canvas mainCanvas;

    protected override void Awake()
    {
        base.Awake();
        EnsureLayerRoots();
    }

    public void EnsureLayerRoots()
    {
        var canvas = ResolveMainCanvas();
        if (canvas == null)
        {
            Debug.LogError("缺少 Main Canvas，无法初始化 UIFramework 层级。");
            return;
        }

        EnsureLayerRoot(canvas.transform, UILayer.Persistent, "PersistentRoot", 0);
        EnsureLayerRoot(canvas.transform, UILayer.Panel, "PanelRoot", 1);
        EnsureLayerRoot(canvas.transform, UILayer.Popup, "PopupRoot", 2);
        EnsureLayerRoot(canvas.transform, UILayer.Overlay, "OverlayRoot", 3);
    }

    public GameObject GetSingleUI(UIType type)
    {
        return ShowUI(type)?.GameObject;
    }

    public UIHandle ShowUI(UIType type)
    {
        if (type == null)
        {
            Debug.LogError("请求显示的 UIType 为空。");
            return null;
        }

        EnsureLayerRoots();

        if (uiInstances.TryGetValue(type, out var existing) && existing.GameObject != null)
        {
            existing.GameObject.SetActive(true);
            return existing;
        }

        if (!layerRoots.TryGetValue(type.Layer, out var parent) || parent == null)
        {
            Debug.LogError($"缺少 UI 层级根节点：{type.Layer}");
            return null;
        }

        var prefab = Resources.Load<GameObject>(type.Path);
        if (prefab == null)
        {
            Debug.LogError($"找不到 UI Prefab：UI={type.Name}，Resources 路径={type.Path}");
            return null;
        }

        var ui = Instantiate(prefab, parent);
        ui.name = type.Name;
        var handle = new UIHandle(type, ui);
        uiInstances[type] = handle;
        return handle;
    }

    public bool TryGetUI<T>(UIType type, out T component) where T : Component
    {
        component = null;
        if (type == null || !uiInstances.TryGetValue(type, out var handle) || handle.GameObject == null)
        {
            return false;
        }

        component = handle.GameObject.GetComponent<T>();
        return component != null;
    }

    public void HideUI(UIType type)
    {
        if (type != null && uiInstances.TryGetValue(type, out var handle) && handle.GameObject != null)
        {
            handle.GameObject.SetActive(false);
        }
    }

    public void DestroyUI(UIType type)
    {
        if (type != null && uiInstances.TryGetValue(type, out var handle))
        {
            if (handle.GameObject != null)
            {
                Destroy(handle.GameObject);
            }

            uiInstances.Remove(type);
        }
    }

    public Transform GetLayerRoot(UILayer layer)
    {
        EnsureLayerRoots();
        layerRoots.TryGetValue(layer, out var root);
        return root;
    }

    private Canvas ResolveMainCanvas()
    {
        if (mainCanvas != null)
        {
            return mainCanvas;
        }

        var canvasObject = GameObject.Find("Main Canvas");
        mainCanvas = canvasObject == null ? null : canvasObject.GetComponent<Canvas>();
        return mainCanvas;
    }

    private void EnsureLayerRoot(Transform canvasTransform, UILayer layer, string rootName, int siblingIndex)
    {
        if (layerRoots.TryGetValue(layer, out var existingRoot) && existingRoot != null)
        {
            existingRoot.SetSiblingIndex(siblingIndex);
            return;
        }

        var child = canvasTransform.Find(rootName);
        if (child == null)
        {
            var root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
            child = root.transform;
            child.SetParent(canvasTransform, false);
            var rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        child.SetSiblingIndex(siblingIndex);
        layerRoots[layer] = child;
    }
}
```

- [ ] **Step 3: Run Unity compile**

Run Unity or wait for Editor compile.

Expected: compile reaches current package state. No new `UIManager` API errors.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scripts/UIFramework/UIHandle.cs" "My project/Assets/Scripts/UIFramework/Manager/UIManager.cs"
git commit -m "feat: add layered UI manager"
```

---

### Task 5: Keep PanelManager and BasePanel Compatible

**Files:**
- Modify: `My project/Assets/Scripts/UIFramework/Manager/PanelManager.cs`
- Modify: `My project/Assets/Scripts/UIFramework/UI/BasePanel.cs`

- [ ] **Step 1: Update PanelManager null-safety**

Update `Push` to check `UIManager.Instance`, `GetSingleUI`, and missing prefabs before calling panel lifecycle:

```csharp
var uiManager = UIManager.Instance;
if (uiManager == null)
{
    Debug.LogError("缺少 UIManager，无法打开面板。");
    return;
}

GameObject panelGo = uiManager.GetSingleUI(nextPanel.UIType);
if (panelGo == null)
{
    Debug.LogError($"无法创建面板：{nextPanel.UIType.Name}");
    return;
}
```

- [ ] **Step 2: Update BasePanel null-safety**

Update `OnPause`, `OnResume`, and `OnExit` so each method returns safely when `UITool` or `UIManager` is null. Preserve current `ButtonAnim.OnExit()` behavior.

- [ ] **Step 3: Run compile**

Run Unity compile.

Expected: no errors caused by `PanelManager` or `BasePanel`.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scripts/UIFramework/Manager/PanelManager.cs" "My project/Assets/Scripts/UIFramework/UI/BasePanel.cs"
git commit -m "fix: keep panel stack compatible with layered UI"
```

---

### Task 6: Add Base Scene UI Context and Panel Root Helper

**Files:**
- Create: `My project/Assets/Scripts/UI/BaseSceneUIContext.cs`
- Create: `My project/Assets/Scripts/UI/BaseSceneUIPanelRoot.cs`

- [ ] **Step 1: Create BaseSceneUIContext**

Create a MonoBehaviour with Chinese Header/Tooltip fields for:

```csharp
GameEntry gameEntry;
RuntimeDataService runtimeDataService;
InventoryService inventoryService;
FactionService factionService;
RoundService roundService;
TaskService taskService;
StoryService storyService;
LetterService letterService;
DocumentService documentService;
CityCameraController cityCameraController;
```

Add public properties and a `ResolveMissingReferences()` method that calls `FindFirstObjectByType<T>(FindObjectsInactive.Include)` for missing fields and logs Chinese errors for still-missing required services.

- [ ] **Step 2: Create BaseSceneUIPanelRoot**

Create a MonoBehaviour with:

```csharp
[Header("调试控件")]
[Tooltip("是否显示本面板内的调试按钮和测试入口。正式流程默认关闭。")]
[SerializeField] private bool showDebugControls;

[Header("调试控件根节点")]
[Tooltip("需要随调试开关显示或隐藏的节点。")]
[SerializeField] private GameObject[] debugRoots;
```

Add:

```csharp
public void ApplyContext(BaseSceneUIContext context) { }
public void ApplyDebugVisibility()
```

`ApplyContext` stores context for future panel-specific extension. `ApplyDebugVisibility` sets all `debugRoots` active state to `showDebugControls`.

- [ ] **Step 3: Run compile**

Expected: no compile errors. Inspector-visible fields use Chinese Header and Tooltip.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scripts/UI/BaseSceneUIContext.cs" "My project/Assets/Scripts/UI/BaseSceneUIPanelRoot.cs"
git commit -m "feat: add Base Scene UI context"
```

---

### Task 7: Add Base Scene UI Bootstrap

**Files:**
- Create: `My project/Assets/Scripts/UI/BaseSceneUIBootstrap.cs`

- [ ] **Step 1: Write bootstrap script**

Create `BaseSceneUIBootstrap` with Chinese Header/Tooltip fields:

```csharp
[SerializeField] private BaseSceneUIContext uiContext;
[SerializeField] private UIManager uiManager;
[SerializeField] private bool showDebugControlsOnStart;
```

Define static UI types:

```csharp
private static readonly UIType SharedHudPanel = new UIType("Prefabs/UI/SharedHudPanel", UILayer.Persistent);
private static readonly UIType DeskPanel = new UIType("Prefabs/UI/DeskPanel", UILayer.Panel);
private static readonly UIType StoryPanel = new UIType("Prefabs/UI/StoryPanel", UILayer.Overlay);
private static readonly UIType CityHudPanel = new UIType("Prefabs/UI/CityHudPanel", UILayer.Panel);
private static readonly UIType DocumentPopupPanel = new UIType("Prefabs/UI/DocumentPopupPanel", UILayer.Popup);
private static readonly UIType NewspaperPanel = new UIType("Prefabs/UI/NewspaperPanel", UILayer.Popup);
private static readonly UIType LetterReaderPanel = new UIType("Prefabs/UI/LetterReaderPanel", UILayer.Popup);
```

In `Start()`:

1. Resolve `uiContext`.
2. Resolve `uiManager`.
3. Call `uiManager.EnsureLayerRoots()`.
4. Show `SharedHudPanel`.
5. Show `DeskPanel`.
6. Apply context/debug visibility to created root helpers.

- [ ] **Step 2: Add public methods**

Add methods:

```csharp
public void ShowDesk()
public void ShowCity()
public void ShowStory()
public void HideStory()
public void ShowDocumentPopup()
public void ShowNewspaper()
public void ShowLetterReader()
public void HidePopup(UIType type)
```

Each method uses `UIManager.ShowUI`, `HideUI`, or `DestroyUI`, then applies `BaseSceneUIPanelRoot`.

- [ ] **Step 3: Run compile**

Expected: no compile errors. The bootstrap compiles before prefabs exist because Resources loads happen at runtime.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scripts/UI/BaseSceneUIBootstrap.cs"
git commit -m "feat: add Base Scene UI bootstrap"
```

---

### Task 8: Add Local Prefab Extraction Builder

**Files:**
- Create: `My project/Assets/Editor/BaseSceneUIFrameworkPrefabBuilder.cs`

- [ ] **Step 1: Create builder menu**

Create menu:

```text
Twelve Moons/UIFramework/Rebuild Base Scene UI Prefabs Only
```

Builder rules:

- Load `Assets/Scenes/BaseScene.unity`.
- Save only these objects as prefabs:
  - `DeskPanel`
  - `SharedHudRoot` as `SharedHudPanel`
  - `StoryPanel`
  - new temporary `CityHudPanel` containing copies of `CityCameraControls` and `CityOverlayPanel`
  - `DocumentPopupPanel`
  - `NewspaperPanel`
  - `LetterReaderPanel` copied from the existing `LetterReaderPanel`
- Save to `Assets/Resources/Prefabs/UI`.
- Add `BaseSceneUIPanelRoot` to each prefab root.
- Assign debug roots by child names:
  - `TestPanel`
  - `StoryDebugButtons`
  - `RoundDebugButtons`
  - `SuspicionDebugButtons`
  - `LetterDebugButtons`
- Keep copied RectTransforms unchanged.

- [ ] **Step 2: Ensure generated TMP text heights are non-negative**

Builder must scan all `TMP_Text` under generated prefab roots:

```csharp
var rect = text.GetComponent<RectTransform>();
if (rect != null && rect.sizeDelta.y < 0f)
{
    rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0f);
}
```

- [ ] **Step 3: Save prefabs**

Use:

```csharp
PrefabUtility.SaveAsPrefabAsset(root, targetPath);
```

Expected target paths:

```text
Assets/Resources/Prefabs/UI/DeskPanel.prefab
Assets/Resources/Prefabs/UI/SharedHudPanel.prefab
Assets/Resources/Prefabs/UI/StoryPanel.prefab
Assets/Resources/Prefabs/UI/CityHudPanel.prefab
Assets/Resources/Prefabs/UI/DocumentPopupPanel.prefab
Assets/Resources/Prefabs/UI/NewspaperPanel.prefab
Assets/Resources/Prefabs/UI/LetterReaderPanel.prefab
```

- [ ] **Step 4: Run builder in Unity**

Run menu:

```text
Twelve Moons/UIFramework/Rebuild Base Scene UI Prefabs Only
```

Expected: seven target prefabs exist under `Assets/Resources/Prefabs/UI`.

- [ ] **Step 5: Commit**

```powershell
git add -- "My project/Assets/Editor/BaseSceneUIFrameworkPrefabBuilder.cs" "My project/Assets/Resources/Prefabs/UI"
git commit -m "feat: extract Base Scene UI prefabs"
```

---

### Task 9: Add Prefab Binding Repair in Builder

**Files:**
- Modify: `My project/Assets/Editor/BaseSceneUIFrameworkPrefabBuilder.cs`

- [ ] **Step 1: Repair DeskPanel references**

After saving/copying `DeskPanel`, bind:

- `DeskPanelView.taskPanel` to the TaskPanel from `SharedHudPanel` at runtime through bootstrap, not a scene reference.
- `DeskPanelView.suspicionPanel` to child `SuspicionPanel`.
- `DeskPanelView.letterArea` to child `LetterArea`.
- `DeskPanelView.inventoryPanel` to child `InventoryPanel`.
- `DeskPanelView.sharedActorSlot` to child `SharedActorSlot`.
- `DeskPanelView.documentPopupPanel` is cleared in prefab; bootstrap supplies popup when needed.

- [ ] **Step 2: Repair SharedHudPanel references**

Bind:

- `TaskPanelView.contentRoot`
- `TaskPanelView.rowPrefab`
- `TaskPanelView.emptyText`
- `RoundPanelView` TMP fields and buttons

Use serialized properties by existing field names so private fields remain assigned.

- [ ] **Step 3: Repair popup references**

Bind `DocumentPopupPanelView`, `NewspaperPanelView`, and `LetterAreaView`/`LetterReaderPanel` internal references using child names already present in the scene.

- [ ] **Step 4: Run builder**

Run:

```text
Twelve Moons/UIFramework/Rebuild Base Scene UI Prefabs Only
```

Expected: generated prefabs have no missing internal references for copied children.

- [ ] **Step 5: Commit**

```powershell
git add -- "My project/Assets/Editor/BaseSceneUIFrameworkPrefabBuilder.cs" "My project/Assets/Resources/Prefabs/UI"
git commit -m "fix: repair UIFramework prefab bindings"
```

---

### Task 10: Wire Bootstrap into BaseScene

**Files:**
- Modify: `My project/Assets/Scenes/BaseScene.unity`

- [ ] **Step 1: Add BaseSceneUIContext to UI Manager**

In `BaseScene`, select `UI Manager`.

Add:

```text
BaseSceneUIContext
```

Drag or auto-resolve:

- `GameEntry`
- `RuntimeDataService`
- `InventoryService`
- `FactionService`
- `RoundService`
- `TaskService`
- `StoryService`
- `LetterService`
- `DocumentService`
- `CityCameraController`

Reason: UI prefabs need scene services after being instantiated from Resources.

- [ ] **Step 2: Add BaseSceneUIBootstrap to UI Manager**

Add:

```text
BaseSceneUIBootstrap
```

Assign:

- `UI Context` = same `UI Manager` object's `BaseSceneUIContext`
- `UI Manager` = same `UI Manager` object's `UIManager`
- `Show Debug Controls On Start` = false

Reason: scene startup must create `SharedHudPanel` and `DeskPanel`.

- [ ] **Step 3: Ensure Main Canvas layer roots**

Use `UIManager.EnsureLayerRoots()` by entering Play Mode once, or create roots manually:

```text
Main Canvas/PersistentRoot
Main Canvas/PanelRoot
Main Canvas/PopupRoot
Main Canvas/OverlayRoot
```

Each root RectTransform:

- Anchor Min: `(0, 0)`
- Anchor Max: `(1, 1)`
- Offset Min: `(0, 0)`
- Offset Max: `(0, 0)`
- Pivot: `(0.5, 0.5)`

Reason: Resources UI needs stable layer parents.

- [ ] **Step 4: Save scene**

Save `BaseScene`.

- [ ] **Step 5: Commit**

```powershell
git add -- "My project/Assets/Scenes/BaseScene.unity"
git commit -m "feat: bootstrap Base Scene UIFramework"
```

---

### Task 11: Clean Migrated UI Instances from BaseScene

**Files:**
- Modify: `My project/Assets/Scenes/BaseScene.unity`

- [ ] **Step 1: Remove migrated concrete UI roots**

Remove from `Main Canvas`:

- `DeskPanel`
- `StoryPanel`
- `SharedHudRoot`

Remove from `CityRoot`:

- `CityCameraControls`
- `CityOverlayPanel`

Keep:

- `Main Canvas`
- `PersistentRoot`
- `PanelRoot`
- `PopupRoot`
- `OverlayRoot`
- `UI Manager`
- all non-UI service and registry objects
- all city runtime objects

- [ ] **Step 2: Save scene**

Save `BaseScene`.

- [ ] **Step 3: Run Play Mode smoke**

Open `BaseScene`, enter Play Mode.

Expected:

- `SharedHudPanel` appears under `PersistentRoot`.
- `DeskPanel` appears under `PanelRoot`.
- No duplicate old concrete UI remains under `Main Canvas`.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Scenes/BaseScene.unity"
git commit -m "refactor: remove migrated UI from Base Scene"
```

---

### Task 12: Add Validator

**Files:**
- Create: `My project/Assets/Editor/BaseSceneUIFrameworkValidator.cs`

- [ ] **Step 1: Create validation menu**

Create menu:

```text
Twelve Moons/UIFramework/Validate Base Scene UIFramework
```

Validator checks:

- Target seven prefabs exist.
- Existing row/card prefabs still exist.
- Each target prefab root has `BaseSceneUIPanelRoot`.
- No `TMP_Text` RectTransform has negative `sizeDelta.y`.
- `BaseScene` has `Main Canvas`.
- `Main Canvas` has the four layer roots.
- `BaseScene` no longer contains direct children named `DeskPanel`, `StoryPanel`, or `SharedHudRoot`.
- `CityRoot` no longer contains `CityCameraControls` or `CityOverlayPanel`.

- [ ] **Step 2: Emit Chinese errors**

Each failure uses clear Chinese messages:

```csharp
throw new InvalidOperationException("缺少 UIFramework 层级根节点：PanelRoot");
```

- [ ] **Step 3: Run validator**

Run:

```text
Twelve Moons/UIFramework/Validate Base Scene UIFramework
```

Expected: validator passes after Task 11.

- [ ] **Step 4: Commit**

```powershell
git add -- "My project/Assets/Editor/BaseSceneUIFrameworkValidator.cs"
git commit -m "test: add Base Scene UIFramework validator"
```

---

### Task 13: Verify Formal UI Flows

**Files:**
- Modify only if verification exposes a direct binding bug in this refactor.

- [ ] **Step 1: Run config smoke test**

Unity menu:

```text
Twelve Moons/Tests/Run Config Loader Smoke Test
```

Expected: config loader smoke test logs success.

- [ ] **Step 2: Run runtime smoke tests**

Run existing smoke tests from `Twelve Moons/Tests` menus:

- Runtime data
- Inventory
- Faction
- Round
- Task
- Letter
- Story
- Document
- Desk loop
- City point
- City building
- City side event

Expected: each smoke test logs success or exposes only pre-existing package-resolution blockers.

- [ ] **Step 3: Manual BaseScene Play Mode**

Open `BaseScene`, enter Play Mode, verify:

- Desk and shared HUD appear.
- Task panel refreshes.
- Round panel shows current round and stage.
- Inventory panel shows resources/items.
- Suspicion panel shows factions.
- Letter area shows received letters.
- Document button starts document flow.
- Document options resolve and feedback appears.
- Story button plays queued story.
- Newspaper opens and closes.
- City button enters city UI.
- City camera buttons move view without refreshing city data.
- Returning to desk keeps runtime state.

- [ ] **Step 4: Debug controls**

On each prefab root, enable `显示调试控件`.

Expected: original debug buttons appear and call their existing methods.

- [ ] **Step 5: Commit fixes from verification**

Only if Task 13 required binding fixes:

```powershell
git add -- "My project/Assets/Scripts" "My project/Assets/Resources/Prefabs/UI" "My project/Assets/Scenes/BaseScene.unity"
git commit -m "fix: preserve UIFramework gameplay flows"
```

---

### Task 14: Final Verification and Delivery Notes

**Files:**
- Read: `My project/Assets/Scenes/BaseScene.unity`
- Read: `My project/Assets/Resources/Prefabs/UI`
- Read: `My project/Assets/Scripts/UIFramework`
- Read: `My project/Assets/Scripts/UI`

- [ ] **Step 1: Run validator**

Run:

```text
Twelve Moons/UIFramework/Validate Base Scene UIFramework
```

Expected: pass.

- [ ] **Step 2: Check compile errors**

Run:

```powershell
rg -n "error CS" "My project\Library\Bee" "My project\Logs"
```

Expected: no new errors from this refactor. If package cache still lacks Newtonsoft, report it as a package restore blocker with exact evidence.

- [ ] **Step 3: Check git diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: changes are limited to UIFramework, UI bootstrap/context scripts, local builder/validator, target prefabs, and BaseScene.

- [ ] **Step 4: Prepare final Unity搭建说明**

Final response must include:

- 修改文件。
- 新增脚本。
- 脚本职责。
- 自动生成的 UI Prefab。
- 仍需手动检查的引用。
- 带原因的 Unity 搭建步骤。
- Button OnClick 绑定及验证目的。
- Unity 验证入口和通过标准。
- 是否可以进入下一阶段。


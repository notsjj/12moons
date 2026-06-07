# Base Scene UIFramework 全量重构设计

## 目标

基于现有 `Assets/Scripts/UIFramework`，将 `BaseScene` 中 `Main Canvas` 下的全部正式 UI，以及 `CityRoot` 中的城区 UI 控件迁移为由 UIFramework 动态创建和管理的 Prefab。

所有新建 UI Prefab 必须位于：

`Assets/Resources/Prefabs/UI`

重构必须保留当前 UI 布局、核心功能、业务接口、按钮行为和运行时数据，不修改非 UI 业务判定，不提前新增后续阶段入口。

## 已确认迁移边界

### 迁移为 UIFramework Prefab

- `DeskPanel` 及其桌面 UI。
- `SharedHudRoot` 中的任务栏和回合栏。
- `StoryPanel`。
- `CityRoot` 中的 `CityCameraControls` 和 `CityOverlayPanel`。
- 公文、报纸、信件阅读器等弹窗。
- 当前已有调试 UI；功能保留，但默认隐藏。

### 保留在 BaseScene

- `Main Canvas` 和 UI 层级根节点。
- `UI Manager`。
- `GameEntry`。
- 各类 Service、Registry 和运行时数据对象。
- 城区摄像机、建筑、城区事件和其他非 UI 城区对象。

## 方案选择

采用按功能域拆分 Prefab、由 UIFramework 分层管理的方案。

不采用每个小组件独立成为框架 UI 的方案，因为跨面板引用、排序和生命周期复杂度过高。

不采用整个 Main Canvas 内容合并为单一大 Prefab 的方案，因为无法充分利用 UIFramework 的显示、隐藏和层级管理能力。

## UIFramework 扩展

### UI 层级

扩展 `UIType`，增加明确的 UI 层级：

- `Persistent`：跨场景状态切换持续显示的共享 HUD。
- `Panel`：桌面、城区等主界面。
- `Popup`：公文、报纸、信件阅读器等弹窗。
- `Overlay`：剧情等覆盖层。

### Main Canvas 层级

`UIManager` 在 `Main Canvas` 下维护以下根节点：

```text
Main Canvas
├── PersistentRoot
├── PanelRoot
├── PopupRoot
└── OverlayRoot
```

根节点只负责 UI 层级和排序，不承载业务逻辑。

### 兼容性

- 保留现有 `PanelManager.Push/Pop/PopAll` API。
- 默认 `Push/Pop` 继续管理普通 `Panel`。
- 为 `Persistent`、`Popup`、`Overlay` 增加明确显示和关闭入口。
- 已显示的单例 UI 再次请求时复用已有实例。
- Popup 或 Overlay 关闭后恢复下层 UI 输入。

## Prefab 边界

创建以下 UI Prefab：

```text
Assets/Resources/Prefabs/UI/
├── DeskPanel.prefab
├── SharedHudPanel.prefab
├── StoryPanel.prefab
├── CityHudPanel.prefab
├── DocumentPopupPanel.prefab
├── NewspaperPanel.prefab
└── LetterReaderPanel.prefab
```

保留现有复用 Prefab：

```text
Assets/Resources/Prefabs/UI/
├── TaskRow.prefab
├── InventoryItemCard.prefab
└── FactionSuspicionRow.prefab
```

### DeskPanel

负责桌面主界面：

- 物品栏。
- 质疑度栏。
- 信件区域和信件列表。
- 桌面流程入口按钮。
- `SharedActorSlot`。
- 桌面状态反馈。
- 默认隐藏的桌面调试控件。

公文、报纸和信件阅读器从 DeskPanel 中拆为独立 Popup。

### SharedHudPanel

负责持续显示：

- 任务栏。
- 回合栏。
- 默认隐藏的任务与回合调试控件。

### StoryPanel

负责剧情 Overlay：

- 对话剧情。
- 文本剧情。
- 图片和漫画剧情。
- 提交类剧情。
- 默认隐藏的剧情调试控件。

### CityHudPanel

负责城区 UI：

- 城区任务、质疑度和回合显示。
- 城区摄像机观察位置按钮。
- 城区 UI 控件。

城区摄像机移动只改变观察位置，不刷新城区数据。

### Popup

- `DocumentPopupPanel`：公文显示、选项、提交和反馈流程。
- `NewspaperPanel`：上一回合报纸显示。
- `LetterReaderPanel`：信件详情阅读。

## 启动与生命周期

### BaseSceneUIBootstrap

新增 `BaseSceneUIBootstrap`，挂载在场景 `UI Manager` 上。

职责：

1. 初始化四个 UI 层级根节点。
2. 创建并持续显示 `SharedHudPanel`。
3. 默认显示 `DeskPanel`。
4. 根据流程切换 `DeskPanel` 与 `CityHudPanel`。
5. 剧情开始时显示 `StoryPanel` Overlay，剧情结束后关闭。
6. 根据业务流程显示和关闭 Popup。
7. 检查必要 UI 和业务服务引用，缺失时输出中文错误。

### BaseSceneUIContext

新增 `BaseSceneUIContext`，集中保存 UI 所需的非 UI 场景服务：

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

所有 Inspector 可见字段必须使用中文 `Header` 和中文 `Tooltip`。

启动时优先使用 Inspector 引用；引用缺失时允许使用 `FindFirstObjectByType` 补全，并输出清晰中文错误或警告。核心 UI 组件不得依赖 `GameObject.Find` 和对象名称查找。

## 绑定与通信

### Prefab 内部绑定

- Prefab 内部 View、Button、TMP 文本、Image 和布局引用继续使用 Inspector 序列化绑定。
- 保留现有 View 脚本公开方法，避免破坏已有 Button OnClick 和运行时调用。
- 所有 Unity UI 文本必须继续使用 TextMeshPro。
- 所有 TMP 文本 RectTransform 高度必须为零或正数。

### 跨 Prefab 通信

- 业务 Service 从 `BaseSceneUIContext` 获取。
- UI 实例从 `UIManager` 的实例查询接口获取。
- 跨 UI 刷新和显示切换使用显式接口。
- 不通过场景对象名称查找核心组件。

### 业务保护

- 不修改任务、公文、剧情、信件、回合、质疑度、背包和城区业务判定。
- 不删除已有运行时 API。
- 建筑不触发剧情。
- 支线任务继续由 `StoryConfig.TriggerTaskId` 在剧情结束后触发。
- `SharedActorSlot` 继续被公文前角色和新公文提出者共用。

## 调试 UI

- 原有调试按钮和测试入口保留。
- 调试 UI 留在所属功能 Prefab 内。
- 默认隐藏，不作为正式流程入口展示。
- 每个所属 Prefab 根节点提供中文说明的 `显示调试控件` Inspector 开关。
- 调试配置、可调参数和测试字段继续使用中文 `Header` 和中文 `Tooltip`。
- 不为仅观察运行时状态新增正式 UI；需要观察的状态优先放入中文只读 Inspector 快照或 Debug View。

## 错误处理

- Resources Prefab 路径不存在时，输出包含 `UIType` 和完整 Resources 路径的中文错误。
- Prefab 缺少必要 View 时，不显示该 UI，并输出缺失组件名称。
- 场景必要 Service 缺失时，Bootstrap 输出中文错误，不静默失败。
- 重复显示同一单例 UI 时复用实例。
- 关闭 Popup 或 Overlay 时恢复正确的下层输入状态。
- 验证工具发现负数文本高度、缺失引用或遗留场景 UI 时输出明确错误。

## 迁移顺序

1. 扩展 UIFramework 层级、实例查询和兼容 API。
2. 创建 `BaseSceneUIContext` 与 `BaseSceneUIBootstrap`。
3. 从当前 BaseScene 原样提取功能域 Prefab，保留布局和内部引用。
4. 将公文、报纸、信件阅读器拆为独立 Popup。
5. 补充显式跨 Prefab 通信与上下文绑定。
6. 清理 BaseScene 中已迁移 UI，只保留 Canvas、层级根节点和 UI Manager。
7. 添加 Editor 验证工具。
8. 完成编译、运行和完整流程验证。

## Editor 验证工具

新增局部 UIFramework 重构验证工具，仅检查和修复本次迁移范围，不运行会重建整个桌面布局的旧总构建器。

验证内容：

- 所有目标 Prefab 路径存在。
- Prefab 根组件和必要 View 存在。
- 必要 Button、TMP 文本和序列化引用不为空。
- BaseScene 不再残留已迁移的具体 UI。
- Main Canvas 四层根节点存在且顺序正确。
- 所有 TMP 文本 RectTransform 高度为零或正数。
- 调试控件默认隐藏。

## 验收标准

- Unity 项目无本次重构相关编译错误。
- BaseScene 启动后自动显示桌面和共享 HUD。
- 任务、回合、物品栏、质疑度、信件刷新正常。
- 剧情的对话、文本、图片、漫画和提交流程正常。
- 公文队列、选项、提交、角色滑入和反馈正常。
- 报纸和信件阅读器正常打开与关闭。
- 进入城区只切换 UI 和摄像机观察位置，不刷新城区数据。
- 返回桌面后运行时数据保持。
- 调试控件默认隐藏，开启中文 Inspector 开关后可使用。
- 所有新 UI Prefab 位于 `Assets/Resources/Prefabs/UI`。
- BaseScene 不再直接保存具体 UI 内容。
- 所有 UI 文本使用 TMP，且文本 RectTransform 高度没有负数。

## Unity 搭建结果要求

实施完成后的说明必须列出：

- 修改文件和新增脚本。
- 每个脚本职责。
- 自动生成的 UI Prefab。
- 仍需手动检查的 Inspector 引用。
- 每个 GameObject、脚本和引用的操作与原因。
- Button OnClick 绑定和验证目的。
- Unity 验证入口和通过标准。
- 是否可以进入下一阶段。

# UI Prefab 中文重命名设计

## 目标

将 `Assets/Resources/Prefabs/UI` 下现有 UI Prefab 的资源文件名、Prefab 根物体名、运行时实例名统一改为中文，并同步所有代码和 Editor 工具中的资源路径引用。

## 范围

仅处理 `Resources/Prefabs/UI` 下已经存在的 UI Prefab。保持现有 UI 布局、组件、绑定和运行逻辑不变，不运行会重建桌面界面的 Builder。

## 命名映射

- `CityHudPanel` -> `城区HUD面板`
- `DeskPanel` -> `桌面面板`
- `DocumentPopupPanel` -> `公文弹窗面板`
- `FactionSuspicionRow` -> `阵营质疑行`
- `InventoryItemCard` -> `物品卡片`
- `LetterReaderPanel` -> `信件阅读面板`
- `LoadingPanel` -> `加载过场面板`
- `NewspaperPanel` -> `报纸面板`
- `SharedHudPanel` -> `共享HUD面板`
- `StoryPanel` -> `剧情面板`
- `TaskRow` -> `任务行`

## 代码关联

运行时 `UIType` 继续通过 `Resources.Load` 加载资源，但路径改为中文资源路径，例如 `Prefabs/UI/桌面面板`。Editor 校验、SmokeTest 和美术样式工具中的 AssetDatabase 路径同步改为中文 `.prefab` 文件路径。

## 验证

验证重点是所有中文路径可被 `Resources.Load` 和 `AssetDatabase.LoadAssetAtPath` 找到，且 `UIType.Name` 从中文路径正确得到中文实例名。

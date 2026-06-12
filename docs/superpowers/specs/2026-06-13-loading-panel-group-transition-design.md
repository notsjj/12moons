# LoadingPanel 成组滑动过场设计

## 目标

重写 `LoadingPanelTransitionView`，让左右图层按各自容器中的 Hierarchy 顺序一一配对成组。无法配对的剩余图层各自成为单独一组。各组从屏幕外依次滑入，全部进入后停顿一秒，再按反向组序原路滑出。

## 范围

- 只修改 LoadingPanel 过场行为、对应测试和运行时调试入口。
- 不修改 LoadingPanel Prefab 层级、图片、RectTransform 布局或其它 UI。
- 保留现有 `PlayEnterCityTransition(Action onCovered, Action onCompleted)` 公共接口及城区切换流程。

## 分组与排序

- 读取 `左侧图层` 和 `右侧图层` 容器下直接拥有 `Image` 的子物体。
- 左右列表分别按 sibling index 从小到大排序。
- `左[0] + 右[0]`、`左[1] + 右[1]` 依次组成组。
- 数量不等时，剩余图层各自单独成组。
- 排序靠前的组视觉上更靠下，先滑入、后滑出。

## 动画时序

1. 显示 LoadingPanel，并将所有图层放到各自屏幕外起点。
2. 使用 DOTween Sequence，按组序依次启动滑入；同组左右图层同步移动。
3. 全部图层到达 Prefab 保存的覆盖位置后停顿 `1 秒`。
4. 调用 `onCovered`，保持现有城区切换时机。
5. 按反向组序依次启动原路滑出；同组左右图层同步移动。
6. 完成后调用 `onCompleted`，恢复隐藏状态。

所有 Tween 使用不受 `Time.timeScale` 影响的更新方式。重播或禁用组件时终止旧 Sequence，避免动画叠加。

## 调试与 Inspector

- 保留现有 Inspector 编辑器预览入口。
- 在始终激活的 `BaseSceneUIBootstrap` 中监听 `P` 键。
- 按下 `P` 时显示 LoadingPanel 并播放纯调试过场，不切换城区。
- 所有新增可调参数、调试开关和运行时快照使用中文 Header 与 Tooltip。
- 调试快照显示解析图层数、左右图层数、组数与当前播放状态。

## 验证

- Smoke Test 验证当前 Prefab 可解析出左右图层和正确组数。
- 验证组序中排序靠前组先进入、最后退出。
- 验证默认覆盖停顿时长为一秒。
- Unity Play 模式按 `P`，确认图层成组进入、停顿、反向原路退出，且不切换城区。


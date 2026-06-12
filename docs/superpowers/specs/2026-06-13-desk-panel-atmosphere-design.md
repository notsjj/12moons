# DeskPanel 暗角与蜡烛光晕设计

## 目标

为 DeskPanel 增加接近参考图的矩形屏幕内侧暗角，并在现有“蜡烛”图片后方增加柔和白色光晕和轻微呼吸闪烁。

## 范围

- 只修改 DeskPanel Prefab 自身及其直接必要子物体、脚本、Shader 和局部更新工具。
- 不改变现有蜡烛位置、DeskPanel 其它 UI 的 RectTransform、层级、颜色、字体、Prefab 或绑定。
- 不运行桌面总构建器，不新增正式按钮或其它阶段入口。

## 实现

- 新增程序化 UI Shader，通过模式参数分别渲染沿矩形屏幕四边向内渐隐的暗角和椭圆径向光晕。
- 新增 `DeskPanelAtmosphereView`：
  - 持有暗角 Image、蜡烛 RectTransform 和光晕 Image 引用。
  - 在运行时创建独立材质实例并同步 Inspector 参数。
  - 使用 DOTween 对光晕透明度和缩放做不受 Time.timeScale 影响的循环呼吸动画。
  - 所有 Inspector 字段提供中文 Header 和 Tooltip。
- 新增 `DeskPanelAtmosphereOnlyBuilder`：
  - 只打开并更新 `Assets/Resources/Prefabs/UI/DeskPanel.prefab`。
  - 在 DeskPanel 根节点下新增全屏 `桌面暗角`，放置在最上层且关闭 Raycast Target。
  - 在现有“蜡烛”前一个 sibling 位置新增 `蜡烛光晕`，确保光晕位于蜡烛后方。
  - 添加并绑定 `DeskPanelAtmosphereView`。

## 验证

- Smoke Test 检查暗角和光晕都存在、关闭射线阻挡、层级关系正确、引用完整。
- 编译检查无错误。
- Unity Play 模式确认屏幕四周渐暗，中心区域清晰，蜡烛周围有柔和暖光并轻微呼吸。

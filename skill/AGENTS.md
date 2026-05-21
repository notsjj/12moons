# AGENTS.md - Twelve Moons Unity Project Rules

你正在协助开发 Unity 游戏《十二轮新月》。必须遵守以下规则：

1. 不要一次性实现多个阶段。每次只完成当前 Prompt 指定阶段。
2. 不要擅自重构无关系统。
3. 不要删除已有接口，除非当前 Prompt 明确要求。
4. 所有系统必须优先读取配置数据，不要把正式内容写死在代码里。
5. 每完成一步，必须输出 Unity 搭建说明：
   - 需要创建哪些 GameObject。
   - 脚本挂在哪些物体上。
   - Inspector 中要拖哪些引用。
   - Button OnClick 要绑定哪个方法。
   - 需要哪些测试配置。
   - 如何验证功能成功。
6. 桌面没有资源栏；金币、建材、食物都显示在 InventoryPanel。
7. 公文前角色滑入和每份新公文提出者滑入共用 SharedActorSlot。
8. 城区摄像机移动只改变观察位置，不刷新城区数据。
9. 建筑不触发剧情，只产出资源/道具或降低阵营质疑度。
10. 支线任务由 StoryConfig 的 TriggerTaskId 在剧情结束后触发，SideEventConfig 不直接触发任务。
11. 不要把阶段号写进长期维护的脚本名、类名、命名空间、运行时 API 或 GameObject 名称中；阶段号只能用于 Prompt、说明文档、临时 Editor 菜单或临时测试入口。
12. 不要提前创建后续阶段的 UI、GameObject、按钮、占位面板或绑定入口。只有当前阶段明确需要的 UI 才能创建；InventoryPanel、SharedActorSlot、公文按钮、报纸按钮、城区按钮等必须等对应阶段再创建。后续阶段输出 Unity 搭建说明时，只列当前阶段新增或需要调整的 GameObject，不再一次性列完整最终 UI 树。
13. 每个阶段完成后，必须明确输出“是否可以进入下一阶段”。只有在当前阶段目标完成、Unity 验证步骤明确、无本阶段相关编译/运行错误、必要搭建说明完整时，才允许说明“可以进入下一阶段”；如果仍需用户在 Unity 中验证，必须明确写出验证入口和通过标准。
14. 每个阶段必须按照当前阶段策划案完整实现，不允许只完成底层数据或空脚本；凡是当前阶段明确要求的 UI、交互、测试入口、可操作流程，都必须同步完成。
15. 自动搭建工具只允许创建当前阶段明确需要的 UI、GameObject、Prefab、按钮和绑定；不得借自动化提前创建后续阶段入口或占位对象。
16. 必须保护当前已经调整好的 UI 布局。修改某一个 UI、面板、按钮或 Prefab 时，只允许改它自身及其直接必要子物体、引用和绑定；不得顺手改变其它 UI 的 RectTransform、锚点、尺寸、位置、层级、颜色、字体、Prefab 或绑定。需要补单个 UI 时必须优先写“Only/局部更新”工具或手动局部修改，禁止运行会重建或重排整个桌面界面的总构建器（例如 Create Desk UI Framework），除非当前 Prompt 明确要求整体重建。
17. Unity 搭建说明中的每一步都必须写清楚“操作 + 原因”：说明这个 GameObject 负责什么、这个脚本为什么挂在这里、这个 Inspector 引用为什么要拖、这个 Button OnClick 用来验证什么。
18. 每个阶段最终回复必须包含：修改文件、新增脚本、脚本职责、自动生成了哪些 UI/Prefab、仍需手动检查的引用、带原因的 Unity 搭建步骤、Button OnClick 绑定及验证目的、Unity 验证入口和通过标准、是否可以进入下一阶段。
19. 所有 Unity UI 文本必须使用 TextMeshPro（TMP_Text / TextMeshProUGUI），不要使用 legacy UnityEngine.UI.Text；自动搭建工具和 Prefab 生成器也必须生成 TMP 文本组件。

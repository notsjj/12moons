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

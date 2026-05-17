# 用 Codex 完成《十二轮新月》的详细操作流程

## 1. 推荐工作方式

不要让 Codex 一次性“做完整游戏”。必须分阶段做，每次只让它完成一个系统，并要求它输出 Unity 搭建说明和测试方法。

推荐工作流：

```text
网页 GPT 负责：规划、md 文档、表格设计、Prompt 设计、验收标准。
Codex App 负责：在 Unity 项目中写代码、改代码、补 UI 脚本。
你负责：按 Codex 的说明在 Unity 中创建对象、拖引用、运行测试。
```

## 2. 每次给 Codex 的固定流程

1. 打开 Codex App，并打开 Unity 项目目录。
2. 确认项目根目录有 AGENTS.md。
3. 把对应阶段的 Prompt 发给 Codex。
4. 要求 Codex 只做当前阶段，不要提前实现后续系统。
5. Codex 修改代码后，先看它的总结。
6. 按它给出的 Unity UI 搭建说明创建对象、挂脚本、拖引用。
7. 运行 Unity，按测试步骤验证。
8. 如果报错，把 Console 错误完整复制回 Codex，让它修复。
9. 阶段通过后，再进入下一阶段。

## 3. 每一步验收必须问 Codex

每个阶段结束时，都要让 Codex 回答：

- 修改了哪些文件？
- 新增了哪些脚本？
- 每个脚本的职责是什么？
- Unity 里要创建哪些对象？
- 脚本挂在哪？
- Inspector 里要拖哪些引用？
- Button OnClick 要绑定哪些方法？
- 用哪些测试数据？
- 如何确认功能成功？

## 4. 建议开发顺序

1. Unity 项目结构、插件、基础场景。
2. 配置表读取系统。
3. 运行时数据系统。
4. 资源/道具系统与物品栏。
5. 阵营质疑度系统。
6. 回合系统 + 灾难阶段判断。
7. 任务系统 + 任务阶段系统。
8. 剧情系统：文字、图片、对话一起做。
9. 信件系统与信件区域。
10. 桌面 UI 框架。
11. 公文系统：显示、选择、结算。
12. 公文抽取系统：任务、后续、灾难、全局。
13. 后续公文激活系统。
14. 城区摄像机移动系统。
15. 城区点位系统。
16. 建筑解锁与建筑点击系统。
17. 支线事件系统。
18. 报纸/回合结算系统。
19. 3 回合完整测试流程。
20. 扩展到 18 回合。
21. UI 动画、音效、存档、Debug 工具。

## 5. 建议项目文件夹

```text
Assets/
├── Art/
├── Audio/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Config/
│   ├── Runtime/
│   ├── Inventory/
│   ├── Faction/
│   ├── Round/
│   ├── Task/
│   ├── Story/
│   ├── Letter/
│   ├── Document/
│   ├── City/
│   ├── Newspaper/
│   └── UI/
└── StreamingAssets/
    └── Configs/
```

## 6. 配表建议

第一版建议用 Excel 编辑，再导出 CSV 或 JSON 给 Unity 读取。运行时不要直接读 xlsx。

业务系统不要直接依赖具体读取方式，而是统一通过 ConfigManager 获取数据。

这样以后如果接入 DataBacker，只需要替换 ConfigProvider，不需要重写公文、任务、剧情等系统。

## 7. 插件建议

强烈建议：

- TextMeshPro：所有文本 UI 使用。
- DOTween：UI 滑入、淡入、按钮反馈、摄像机移动。
- Newtonsoft Json：如果使用 JSON 配置。

可选：

- DataBacker：如果你希望后期更方便管理大量表格。
- Odin Inspector：如果你希望 Inspector 调试更舒服。

不要一开始就让所有系统写死依赖 DataBacker。先保留 IConfigProvider 接口。
# 《十二轮新月》Codex 开发资料包

这个资料包用于配合 Codex App 开发 Unity 项目。

## 文件内容

```text
docs/
├── 00_最终对齐说明.md
├── 01_完整流程说明.md
├── 02_配置表系统设计.md
├── 03_Unity_UI搭建总规范.md
├── 04_Codex详细操作流程.md
└── 05_Codex阶段Prompt索引.md

prompts/
└── phase_00 到 phase_20 的分阶段 Codex Prompt

skill/
├── AGENTS.md
└── 十二轮新月_Codex项目Skill.md
```

## 使用方式

1. 把 `skill/AGENTS.md` 放到 Unity 项目根目录。
2. 把 `docs/` 文件夹放到项目根目录或单独保存。
3. 每次只复制 `prompts/` 中一个阶段的 Prompt 给 Codex。
4. Codex 完成后，按它输出的 Unity UI 搭建说明创建对象、挂脚本、拖引用。
5. 测试通过后再进入下一阶段。

## 配表

配表模板见单独的 Excel 文件：

`十二轮新月_配置表模板_含示例.xlsx`

该工作簿包含 14 张核心配置表，每张表都有字段、字段说明和一行示例数据。

---

# 《十二轮新月》最终对齐说明

## 1. 核心定位

《十二轮新月》是一个以固定灾难回合为外层框架、以桌面公文决策为核心玩法、以任务阶段推进剧情与公文内容、以 3D 城区模型承载建筑与支线点位的叙事经营游戏。

核心结构：

```text
固定灾难回合框架
+
任务阶段驱动剧情、公文前角色、任务公文和信件
+
灾难阶段决定灾难公文池
+
桌面公文决策
+
阵营质疑度反馈
+
文字信件区域
+
物品栏管理金币/建材/食物/任务道具/角色道具
+
3D 城区模型 + 摄像机移动查看不同城区
+
城区点位支线事件
+
剧情结束触发任务或给予道具
```

## 2. 最重要的系统边界

### 灾难阶段

灾难阶段只负责决定当前回合从哪个灾难公文池里抽取灾难公文。

灾难阶段不负责任务阶段、任务剧情、信件发放、支线事件、建筑解锁或城区表现。

### 任务阶段

任务阶段只属于某个任务内部，负责：

- 该任务阶段从任务开始后的第几回合开始。
- 该任务阶段从任务开始后的第几回合结束。
- 回合开始前剧情。
- 回合结束剧情。
- 处理公文前滑入角色。
- 点击滑入角色后播放的剧情。
- 阶段关联公文。
- 阶段给的信件。

任务阶段的 StartOffsetRound / EndOffsetRound 是相对于任务激活回合的偏移。

例：任务第 3 回合激活，阶段 StartOffsetRound = 0，则阶段在第 3 回合开始；EndOffsetRound = 2，则阶段在第 5 回合结束时结束。

## 3. 桌面界面

桌面是主操作界面。

桌面包含：

```text
DeskPanel
├── TaskPanel              任务栏
├── SuspicionPanel         阵营质疑度栏
├── LetterArea             信件区域，可显示多封信件
├── InventoryPanel         物品栏
├── DocumentButton         公文按钮
├── CityButton             城区按钮
├── NewspaperButton        报纸按钮
├── SharedActorSlot        公文前角色与每份新公文提出者共用的角色滑入位置
└── DocumentPopupPanel     公文弹出面板
```

没有单独资源栏。金币、建材、食物都作为 ItemConfig 中的道具显示在物品栏里。

信件不是单个按钮，而是 LetterArea 中的多封信件卡片。

## 4. 公文规则

每份公文固定两个选项。公文配置为一张大表 DocumentConfig，不拆 DocumentOptionConfig。

公文不触发剧情。公文选择只产生：

- 提出者反馈。
- 阵营反馈。
- 物品变化。
- 阵营质疑度变化。
- 任务分变化。
- 后续公文记录。
- 建筑解锁。
- 回合结算文本。

一份任务公文只主要影响一个 TaskId。

公文前角色剧情和每份新公文提出者的角色滑入使用同一个 UI 位置 SharedActorSlot。

## 5. 剧情规则

剧情类型包括 Dialogue、Image、Text。

剧情入口只有：

1. 回合开始前剧情。
2. 回合结束时剧情。
3. 处理公文前角色点击剧情。
4. 城区支线角色点击剧情。

剧情结束后可以触发任务或给予道具。

## 6. 对话规则

DialogueConfig 参考既有 CSV 的方式：

- Content 用 `|` 分隔选项文本。
- NextLineId 用 `|` 分隔对应跳转目标。
- Position 只有 0 和 1，0 表示左侧，1 表示右侧。
- 没有中间位置。
- 如果当前行没有新角色覆盖某一侧，则该侧继续显示上一行角色。

## 7. 城区规则

城区是一个 3D 模型场景。

城区切换不是刷新数据，而是移动摄像机到不同空物体位置，相当于放大查看某一区域。

进入城区时，所有当前应显示的建筑、支线角色、点位状态已经统一生成好。

摄像机移动只改变观察位置，不刷新城区数据。

## 8. 建筑规则

建筑不会触发剧情。

建筑作用只有两类：

1. 产出资源/道具。
2. 降低某个阵营质疑度。

建筑位置固定写在 CityBuildingConfig 中。公文只负责解锁 BuildingId。

## 9. 支线规则

SideEventConfig 只负责哪一回合、哪个点位、出现哪个角色、点击后播放哪个剧情。

支线表不直接触发任务。支线任务由 StoryConfig 的 TriggerTaskId 在剧情结束后统一触发。

---

# 《十二轮新月》完整流程说明

## 1. 最外层流程

```text
游戏开始
 ↓
进入当前灾难
 ↓
初始化当前回合、玩家道具、阵营质疑度、任务状态、信件列表、建筑状态、支线事件状态
 ↓
进入第 1 回合
 ↓
循环执行回合流程
 ↓
直到灾难总回合结束
 ↓
进入灾难结算或后续内容
```

## 2. 单回合流程

```text
进入新回合
 ↓
读取当前回合数
 ↓
判断当前灾难阶段
 ↓
检查当前回合应激活的任务阶段
 ↓
发放任务阶段开始信件
 ↓
播放任务阶段开始剧情
 ↓
进入桌面界面
 ↓
显示任务栏、质疑度栏、信件区域、物品栏、公文按钮、城区按钮、报纸按钮
 ↓
如果当前任务阶段配置了处理公文前滑入角色：
    角色滑入 SharedActorSlot
    玩家点击角色
    播放 BeforeDocumentStoryId 对应剧情
    剧情结束后回到桌面
 ↓
玩家点击桌面公文按钮
 ↓
系统生成本回合公文队列
 ↓
每份公文的提出者依次滑入 SharedActorSlot
 ↓
公文在桌面上弹出
 ↓
玩家逐份处理公文
 ↓
每份公文选择后执行结果
 ↓
所有公文处理完成
 ↓
回到桌面
 ↓
玩家可以进入城区
 ↓
城区中所有已解锁建筑、支线角色、点位状态已提前生成好
 ↓
玩家通过按钮移动摄像机查看不同城区
 ↓
玩家点击建筑，获得资源/道具或降低阵营质疑度
 ↓
玩家点击支线角色，播放剧情，剧情结束后可能触发任务或给予道具
 ↓
玩家点击结束回合
 ↓
播放当前回合任务阶段结束剧情
 ↓
发放任务阶段结束信件
 ↓
结算任务阶段/任务结果
 ↓
生成本回合报纸内容
 ↓
当前回合 +1
 ↓
进入下一回合
```

## 3. 公文队列生成流程

本回合公文队列由四类组成：

1. 当前任务阶段关联公文。
2. 已到激活回合的后续公文。
3. 当前灾难阶段的灾难公文。
4. 全局随机公文。

推荐生成顺序：

```text
加入当前任务阶段 LinkedDocumentIds 中的任务公文
 ↓
加入本回合到期的后续公文
 ↓
根据当前灾难阶段随机抽取灾难公文
 ↓
随机抽取全局公文
 ↓
组成最终公文队列
```

任务公文和后续公文优先，剩余位置由灾难公文和全局公文补足。

## 4. 公文处理流程

```text
显示公文标题、正文、提出者、选项 A、选项 B
 ↓
玩家点击选项
 ↓
检查选项是否需要道具
 ↓
如果道具不足，禁止执行或按钮置灰
 ↓
如果道具足够，执行选项结果：
    消耗道具
    获得道具
    修改金币/建材/食物
    修改四阵营质疑度
    修改 TaskId 对应任务的任务分
    记录后续公文激活回合
    解锁建筑
    记录 ResultText 到本回合结算
    显示 ProposerFeedbackText
    在质疑度栏显示 FactionFeedbackText
 ↓
进入下一份公文
```

## 5. 城区流程

```text
玩家进入城区
 ↓
系统根据当前状态生成/显示所有已解锁建筑、可出现支线角色、点位图标
 ↓
玩家点击城区按钮
 ↓
摄像机移动到对应城区空物体位置
 ↓
玩家查看该城区
 ↓
玩家点击建筑或支线角色
 ↓
执行对应逻辑
```

摄像机移动只负责查看，不刷新数据。

## 6. 支线流程

```text
进入城区
 ↓
SideEventSystem 检查当前回合
 ↓
找出符合条件的 SideEvent
 ↓
根据 PointId 找到场景点位
 ↓
在点位生成支线角色图标
 ↓
玩家点击角色图标
 ↓
播放 StoryId 对应剧情
 ↓
剧情结束
 ↓
StoryConfig 检查是否触发新任务或给予道具
```

---

# 《十二轮新月》配置表系统设计

第一版使用 14 张核心配置表。公文选项合并进 DocumentConfig；对话选项合并进 DialogueConfig；建筑位置固定在 CityBuildingConfig；支线任务由 StoryConfig 在剧情结束后触发。

## DisasterConfig

| 字段 | 说明 |
|---|---|
| DisasterId | 灾难ID |
| DisasterName | 灾难名称 |
| TotalRound | 总回合数 |
| Description | 灾难描述 |
| Remark | 备注 |

## DisasterStageConfig

| 字段 | 说明 |
|---|---|
| DisasterStageId | 灾难阶段ID |
| DisasterId | 所属灾难ID |
| StageName | 阶段名称 |
| StartRound | 开始回合 |
| EndRound | 结束回合 |
| Remark | 备注 |

## TaskConfig

| 字段 | 说明 |
|---|---|
| TaskId | 任务ID |
| TaskName | 任务名称 |
| TaskType | Main/Small/Faction/Side |
| Description | 任务描述 |
| StartRound | 任务开始回合，可空 |
| EndRound | 任务结束回合，可空 |
| SuccessScore | 成功所需分数 |
| FailScore | 失败阈值，可空 |
| SuccessResultText | 成功结算文本 |
| FailResultText | 失败结算文本 |
| ShowInTaskPanel | 是否显示在任务栏 |
| Remark | 备注 |

## TaskStageConfig

| 字段 | 说明 |
|---|---|
| TaskStageId | 任务阶段ID |
| TaskId | 所属任务ID |
| StageIndex | 阶段序号 |
| StartOffsetRound | 相对任务激活回合的阶段开始偏移 |
| EndOffsetRound | 相对任务激活回合的阶段结束偏移 |
| StartStoryId | 回合开始前剧情 |
| EndStoryId | 回合结束剧情 |
| BeforeDocumentCharacterId | 处理公文前滑入角色 |
| BeforeDocumentStoryId | 点击滑入角色触发剧情 |
| StartLetterId | 阶段开始给的信件 |
| EndLetterId | 阶段结束给的信件 |
| LinkedDocumentIds | 阶段关联公文ID，多个用 | |
| StageDescription | 阶段描述 |
| Remark | 备注 |

## DocumentConfig

| 字段 | 说明 |
|---|---|
| DocumentId | 公文ID |
| Title | 公文标题 |
| BodyText | 公文正文 |
| ProposerCharacterId | 提出者角色ID |
| DocumentType | Global/Disaster/Task/FollowUp |
| DisasterId | 灾难ID，可空 |
| DisasterStageId | 灾难阶段ID，可空 |
| TaskId | 关联任务ID，可空 |
| TaskStageId | 关联任务阶段ID，可空 |
| IsRepeatable | 是否可重复抽取 |
| Remark | 备注 |
| OptionA_Text | 选项A文本 |
| OptionB_Text | 选项B文本 |
| OptionA_MoneyChange | A金币变化 |
| OptionB_MoneyChange | B金币变化 |
| OptionA_MaterialChange | A建材变化 |
| OptionB_MaterialChange | B建材变化 |
| OptionA_FoodChange | A食物变化 |
| OptionB_FoodChange | B食物变化 |
| OptionA_NobleSuspicionChange | A贵族质疑变化 |
| OptionB_NobleSuspicionChange | B贵族质疑变化 |
| OptionA_AcademySuspicionChange | A学院质疑变化 |
| OptionB_AcademySuspicionChange | B学院质疑变化 |
| OptionA_ChurchSuspicionChange | A教会质疑变化 |
| OptionB_ChurchSuspicionChange | B教会质疑变化 |
| OptionA_CivilianSuspicionChange | A平民质疑变化 |
| OptionB_CivilianSuspicionChange | B平民质疑变化 |
| OptionA_TaskScoreChange | A任务分变化，只影响本行 TaskId |
| OptionB_TaskScoreChange | B任务分变化，只影响本行 TaskId |
| OptionA_RequiredItemId | A需要道具 |
| OptionB_RequiredItemId | B需要道具 |
| OptionA_RequiredItemCount | A需要数量 |
| OptionB_RequiredItemCount | B需要数量 |
| OptionA_ConsumeItem | A是否消耗 |
| OptionB_ConsumeItem | B是否消耗 |
| OptionA_AddItemId | A获得道具 |
| OptionB_AddItemId | B获得道具 |
| OptionA_AddItemCount | A获得数量 |
| OptionB_AddItemCount | B获得数量 |
| OptionA_NextDocumentId | A触发后续公文ID |
| OptionB_NextDocumentId | B触发后续公文ID |
| OptionA_NextDocumentDelayRound | A延迟几回合 |
| OptionB_NextDocumentDelayRound | B延迟几回合 |
| OptionA_UnlockBuildingId | A解锁建筑 |
| OptionB_UnlockBuildingId | B解锁建筑 |
| OptionA_ResultText | A结算文本 |
| OptionB_ResultText | B结算文本 |
| OptionA_ProposerFeedbackText | A提出者反馈 |
| OptionB_ProposerFeedbackText | B提出者反馈 |
| OptionA_FeedbackFactionId | A反馈阵营ID |
| OptionB_FeedbackFactionId | B反馈阵营ID |
| OptionA_FactionFeedbackText | A阵营反馈文本 |
| OptionB_FactionFeedbackText | B阵营反馈文本 |

## StoryConfig

| 字段 | 说明 |
|---|---|
| StoryId | 剧情ID |
| StoryName | 剧情名称 |
| StoryType | Dialogue/Image/Text |
| ImageId | 图片剧情用，可空 |
| TextContent | 纯文字剧情用，可空 |
| TriggerTaskOnEnd | 结束后是否触发任务 |
| TriggerTaskId | 结束后触发任务ID |
| AddItemId | 剧情结束获得道具 |
| AddItemCount | 获得数量 |
| Remark | 备注 |

## DialogueConfig

| 字段 | 说明 |
|---|---|
| LineId | 对话行ID |
| StoryId | 所属剧情ID |
| NextLineId | 下一行ID；如果是选项，用 idA|idB |
| SpeakerCharacterId | 当前说话角色ID |
| Content | 文本；如果是选项，用 选项A|选项B |
| Position | 0左，1右 |
| IsChoice | 是否为选项行 |
| RequiredItemIds | 选项需要道具，多个用 | |
| RequiredItemCounts | 选项需要数量，多个用 | |
| ConsumeItems | 选项是否消耗，多个用 | |
| AddItemIds | 选项获得道具，多个用 | |
| AddItemCounts | 选项获得数量，多个用 | |
| Remark | 备注 |

## CharacterConfig

| 字段 | 说明 |
|---|---|
| CharacterId | 角色ID |
| CharacterName | 角色名称 |
| FactionId | 所属阵营ID |
| PortraitId | 立绘ID |
| Description | 角色简介 |
| Remark | 备注 |

## ItemConfig

| 字段 | 说明 |
|---|---|
| ItemId | 道具ID |
| ItemName | 道具名称 |
| ItemType | Money/Material/Food/TaskItem/Character |
| Description | 描述 |
| IconId | 图标ID |
| CanDrag | 是否可拖拽 |
| CanConsume | 是否可消耗 |
| Remark | 备注 |

## LetterConfig

| 字段 | 说明 |
|---|---|
| LetterId | 信件ID |
| Title | 信件标题 |
| SenderName | 发信人 |
| BodyText | 正文 |
| Remark | 备注 |

## FactionConfig

| 字段 | 说明 |
|---|---|
| FactionId | 阵营ID |
| FactionName | 阵营名称 |
| InitSuspicion | 初始质疑度 |
| MaxSuspicion | 最大质疑度 |
| LowSuspicionThreshold | 低质疑度奖励阈值 |
| LowSuspicionLetterId | 低质疑度奖励信件ID |
| HighSuspicionThreshold | 高质疑度惩罚阈值 |
| PunishTaskId | 高质疑度触发惩罚任务ID，可空 |
| Remark | 备注 |

## CityBuildingConfig

| 字段 | 说明 |
|---|---|
| BuildingId | 建筑ID |
| BuildingName | 建筑名称 |
| CityAreaId | 所属城区ID |
| PointId | 建筑所在点位ID |
| DefaultVisible | 默认是否显示 |
| BuildingEffectType | Resource/Suspicion |
| ProduceItemId | 产出道具ID，可空 |
| ProduceCount | 产出数量 |
| ReduceFactionId | 降低质疑度的阵营ID，可空 |
| ReduceSuspicionValue | 降低质疑度数值 |
| CooldownRound | 冷却回合，可选 |
| Remark | 备注 |

## CityPointConfig

| 字段 | 说明 |
|---|---|
| PointId | 点位ID |
| PointName | 点位名称 |
| CityAreaId | 所属城区ID |
| PointType | Building/SideCharacter/Story/Any |
| DefaultVisible | 默认是否显示 |
| Remark | 备注 |

## SideEventConfig

| 字段 | 说明 |
|---|---|
| SideEventId | 支线事件ID |
| Round | 出现回合 |
| CityAreaId | 出现城区ID |
| PointId | 出现点位ID |
| DisplayCharacterId | 显示角色ID |
| StoryId | 点击后触发剧情ID |
| ExpireRound | 过期回合，可空 |
| IsOneTime | 是否只触发一次 |
| RequiredTaskId | 出现条件：需要任务ID，可空 |
| RequiredTaskState | 出现条件：任务状态，可空 |
| RequiredItemId | 出现条件：需要道具ID，可空 |
| RequiredItemCount | 需要数量 |
| Remark | 备注 |


---

# Unity UI 搭建总规范

## 1. Codex 每一步必须输出的 UI 信息

Codex 每完成一个阶段，都必须在回复中写清楚：

1. 需要在 Unity Hierarchy 中创建哪些 GameObject。
2. 每个 GameObject 的推荐命名。
3. 每个脚本挂在哪个 GameObject 上。
4. Inspector 里需要拖哪些引用。
5. Button OnClick 需要绑定哪个方法。
6. 需要哪些测试数据。
7. 如何手动测试该阶段是否成功。
8. 如果出错，最先检查哪些对象和引用。

## 2. 桌面 UI 结构

推荐结构：

```text
Canvas
└── DeskPanel
    ├── TaskPanel
    ├── SuspicionPanel
    │   ├── NobleSuspicionView
    │   ├── AcademySuspicionView
    │   ├── ChurchSuspicionView
    │   ├── CivilianSuspicionView
    │   └── FactionFeedbackText
    ├── LetterArea
    │   └── LetterCardContainer
    ├── InventoryPanel
    │   └── ItemSlotContainer
    ├── DocumentButton
    ├── CityButton
    ├── NewspaperButton
    ├── SharedActorSlot
    └── DocumentPopupPanel
        ├── TitleText
        ├── BodyText
        ├── OptionAButton
        ├── OptionBButton
        ├── ProposerFeedbackText
        └── StampImage
```

SharedActorSlot 是公文前角色和每份新公文提出者共用的角色滑入位置。

## 3. 剧情 UI 结构

```text
StoryPanel
├── DialoguePanel
│   ├── LeftPortrait
│   ├── RightPortrait
│   ├── SpeakerNameText
│   ├── DialogueText
│   ├── ChoiceButtonA
│   └── ChoiceButtonB
├── ImageStoryPanel
│   ├── StoryImage
│   └── ContinueButton
└── TextStoryPanel
    ├── TextContent
    └── ContinueButton
```

Position = 0 时更新左侧角色；Position = 1 时更新右侧角色。没有新角色覆盖时，保留上一行角色。

## 4. 城区 UI / 场景结构

```text
CityRoot
├── CityModel
├── CameraPoints
│   ├── CameraPoint_Royal
│   ├── CameraPoint_Church
│   ├── CameraPoint_Upper
│   ├── CameraPoint_Lower
│   └── CameraPoint_Outer
├── CityPoints
│   ├── Point_lower_pump_01
│   ├── Point_outer_gate_01
│   └── ...
└── CityCanvas
    ├── CityAreaButtons
    ├── BackToDeskButton
    └── EndRoundButton
```

移动摄像机只改变观察位置，不刷新城区数据。进入城区时由系统统一生成/显示所有可见建筑和支线角色。

---

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

---

# AGENTS.md — 《十二轮新月》Codex 项目工作规范

你正在协助开发 Unity 游戏《十二轮新月》。你必须严格遵守本文件。

## 1. 项目核心规则

- 这是一个固定灾难回合 + 桌面公文决策 + 3D 城区查看 + 任务阶段推进剧情的游戏。
- 灾难阶段只影响灾难公文抽取。
- 任务阶段负责剧情、信件、公文前角色、任务公文。
- 公文固定两个选项，配置在一张 DocumentConfig 大表中。
- 公文不会触发剧情。
- 公文前角色和每份新公文提出者共用 SharedActorSlot 滑入位置。
- 金币、建材、食物都在物品栏显示，不做单独资源栏。
- 信件是 LetterArea 中的多封文字信件卡片。
- 城区切换只移动摄像机，不刷新城区数据。
- 建筑不会触发剧情，只产出资源/道具或降低阵营质疑度。
- 支线任务由 StoryConfig 的 TriggerTaskId 在剧情结束后触发，不由 SideEventConfig 直接触发。

## 2. 每次修改代码的限制

- 只实现用户当前阶段要求的功能。
- 不要提前实现后续阶段。
- 不要大规模重构无关代码。
- 不要删除已有接口，除非用户明确要求。
- 不要把正式数据写死在业务代码里。
- 测试数据可以放在 Demo/Test 脚本或示例配置中。

## 3. 每次完成后必须输出

1. 修改了哪些文件。
2. 新增了哪些脚本。
3. 每个脚本的职责。
4. Unity Hierarchy 中需要创建哪些 GameObject。
5. 每个脚本挂在哪个 GameObject 上。
6. Inspector 中需要拖哪些引用。
7. Button OnClick 需要绑定哪个方法。
8. 需要哪些测试表格或测试数据。
9. 如何在 Unity 中测试。
10. 下一步建议。

## 4. 数据读取原则

所有系统通过 ConfigManager 获取配置，不要直接读取 CSV/JSON/DataBacker。

推荐依赖方向：

```text
业务系统 → ConfigManager → IConfigProvider → CSV/JSON/DataBacker
```

## 5. UI 输出原则

每实现一个系统，都必须告诉用户如何搭建 UI，不允许只写代码不说明 Unity 搭建步骤。

## 6. 编码风格

- C# 脚本职责清晰，一个脚本只负责一个系统或一个 UI 控制器。
- RuntimeData 和 ConfigData 分开。
- Manager 负责流程或数据管理，View/Controller 负责 UI 显示与交互。
- 命名清晰，不使用过度抽象。
- 优先保证可测试、可运行，再考虑表现动画。
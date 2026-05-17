# Table List - 表格中文名与作用

除了 DialogueConfig，其它表通常各一张总表。DialogueConfig 建议按每个剧情复制一张表或导出一个 CSV。

| 英文表名 | 中文名 | 作用 | 使用方式 |
|---|---|---|---|
| `DisasterConfig` | 灾难表 | 定义一个灾难关卡：灾难 ID、名称、总回合数等。 | 一张总表 |
| `DisasterStageConfig` | 灾难阶段表 | 定义每个回合所属灾难阶段；只影响灾难公文抽取。 | 一张总表 |
| `TaskConfig` | 任务表 | 定义任务本体：主线/小任务/势力/支线任务、起止回合、成功失败判定。 | 一张总表 |
| `TaskStageConfig` | 任务阶段表 | 定义任务内部阶段；开始/结束为相对任务激活回合的偏移，并配置阶段剧情、信件和关联公文。 | 一张总表 |
| `DocumentConfig` | 公文大表 | 一行一份公文，固定两个选项；配置资源、道具、质疑度、任务分、后续公文、建筑解锁和反馈。 | 一张总表 |
| `StoryConfig` | 剧情表 | 定义剧情本体：Dialogue/Image/Text；剧情结束后可触发任务或给予道具。 | 一张总表 |
| `DialogueConfig` | 对话模板表 | 对话行结构模板；实际项目建议每个剧情复制一张 Dialogue_xxx 表或导出一个 Dialogue_xxx.csv。 | 每个剧情一张/一个 CSV，复制模板使用 |
| `CharacterConfig` | 角色表 | 定义剧情/公文使用的角色：名称、所属阵营、立绘、描述。 | 一张总表 |
| `ItemConfig` | 道具表 | 定义金币、建材、食物、任务道具、角色道具。 | 一张总表 |
| `LetterConfig` | 信件表 | 定义纯文字信件；任务阶段和阵营低质疑度通过 LetterId 发放。 | 一张总表 |
| `FactionConfig` | 阵营表 | 定义四阵营质疑度初始值、阈值、低质疑信件和高质疑惩罚任务。 | 一张总表 |
| `CityBuildingConfig` | 城区建筑表 | 定义建筑固定点位和建筑效果；建筑只产出资源/道具或降低质疑度，不触发剧情。 | 一张总表 |
| `CityPointConfig` | 城区点位表 | 定义城区点位，与 Unity 场景中 CityPointView 的 PointId 对应。 | 一张总表 |
| `SideEventConfig` | 支线事件表 | 定义哪一回合哪个点位出现哪个支线角色，点击后播放哪个剧情。 | 一张总表 |

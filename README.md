# Twelve Moons Codex Development Package v2

这是《十二轮新月》给 Codex 使用的开发资料包。v2 已修正：

1. 所有文件名改为英文/ASCII，避免 Windows 或部分解压软件出现乱码。
2. README 新增“每张表的中文名和作用”。
3. 明确 DialogueConfig 是模板表，实际项目建议每个剧情单独复制一张对话表或导出一个 CSV。
4. 补齐并统一整理 docs、prompts、skill 目录。

## 推荐使用方式

1. 把本包解压到 Unity 项目根目录，或把 `docs/`、`prompts/`、`AGENTS.md` 复制到项目根目录。
2. 把 `config_templates/TwelveMoons_ConfigTables_v2.xlsx` 作为策划配表模板。
3. 每次只把 `prompts/phase_xx_*.md` 中的一个阶段 Prompt 交给 Codex。
4. Codex 完成后，必须检查它输出的 Unity UI 搭建说明：创建哪些 GameObject、脚本挂哪里、Inspector 拖哪些引用、按钮 OnClick 绑定什么、如何测试。

## 表格总览：英文表名、中文名、作用

| 英文表名 | 中文名 | 作用 |
|---|---|---|
| `DisasterConfig` | 灾难表 | 定义一个灾难关卡：灾难 ID、名称、总回合数等。 |
| `DisasterStageConfig` | 灾难阶段表 | 定义每个回合所属灾难阶段；只影响灾难公文抽取。 |
| `TaskConfig` | 任务表 | 定义任务本体：主线/小任务/势力/支线任务、起止回合、成功失败判定。 |
| `TaskStageConfig` | 任务阶段表 | 定义任务内部阶段；开始/结束为相对任务激活回合的偏移，并配置阶段剧情、信件和关联公文。 |
| `DocumentConfig` | 公文大表 | 一行一份公文，固定两个选项；配置资源、道具、质疑度、任务分、后续公文、建筑解锁和反馈。 |
| `StoryConfig` | 剧情表 | 定义剧情本体：Dialogue/Image/Text；剧情结束后可触发任务或给予道具。 |
| `DialogueConfig` | 对话模板表 | 对话行结构模板；实际项目建议每个剧情复制一张 Dialogue_xxx 表或导出一个 Dialogue_xxx.csv。 |
| `CharacterConfig` | 角色表 | 定义剧情/公文使用的角色：名称、所属阵营、立绘、描述。 |
| `ItemConfig` | 道具表 | 定义金币、建材、食物、任务道具、角色道具。 |
| `LetterConfig` | 信件表 | 定义纯文字信件；任务阶段和阵营低质疑度通过 LetterId 发放。 |
| `FactionConfig` | 阵营表 | 定义四阵营质疑度初始值、阈值、低质疑信件和高质疑惩罚任务。 |
| `CityBuildingConfig` | 城区建筑表 | 定义建筑固定点位和建筑效果；建筑只产出资源/道具或降低质疑度，不触发剧情。 |
| `CityPointConfig` | 城区点位表 | 定义城区点位，与 Unity 场景中 CityPointView 的 PointId 对应。 |
| `SideEventConfig` | 支线事件表 | 定义哪一回合哪个点位出现哪个支线角色，点击后播放哪个剧情。 |

## DialogueConfig 的特殊说明

除了 DialogueConfig，其它 13 张表通常每种只需要一张总表。

DialogueConfig 比较特殊：它是“对话行结构模板”。实际使用时建议：

```text
Dialogue_story_001.csv
Dialogue_story_002.csv
Dialogue_story_side_oldman_01.csv
...
```

也可以在 Excel 里复制多个工作表：

```text
Dialogue_story_001
Dialogue_story_002
Dialogue_side_001
```

每张对话表内部字段一致，使用 `StoryId` 标识所属剧情。这样对话内容多时不会把一张总表变得特别长。

## 目录结构

```text
AGENTS.md
README.md
docs/
prompts/
skill/
config_templates/
```

# DialogueConfig 使用说明

DialogueConfig 是对话行模板，不建议把所有剧情塞进一张超长总表。

## 推荐结构

除了 DialogueConfig，其它 13 张配置表通常各一张即可。DialogueConfig 建议按剧情拆分：

```text
Dialogue_story_001.csv
Dialogue_story_002.csv
Dialogue_side_001.csv
Dialogue_task_main_01_stage_01.csv
```

或者在 Excel 中复制多个工作表：

```text
Dialogue_story_001
Dialogue_story_002
Dialogue_side_001
```

## 字段规则

- `Content`：普通行是台词；选项行用 `|` 分隔多个选项文本。
- `NextLineId`：普通行是下一行 ID；选项行用 `|` 分隔多个跳转目标。
- `Position`：0 = 左侧，1 = 右侧，没有中间。
- `SpeakerCharacterId`：当前说话角色。若某一侧没有新角色覆盖，则沿用上一行该侧角色。
- `IsChoice`：是否为选项行。

## 示例

| LineId | StoryId | NextLineId | SpeakerCharacterId | Content | Position | IsChoice |
|---|---|---|---|---|---:|---|
| line_001 | story_001 | line_002 | npc_a | 你终于来了。 | 0 | FALSE |
| line_002 | story_001 | line_003\|line_008 |  | 询问情况\|直接离开 | 0 | TRUE |
| line_003 | story_001 | END | npc_a | 情况比你想的更糟。 | 0 | FALSE |
| line_008 | story_001 | END | npc_a | 那你就当没来过。 | 0 | FALSE |

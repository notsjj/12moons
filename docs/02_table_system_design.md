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

| ?? | ?? |
|---|---|
| StoryId | ??ID |
| StoryName | ???? |
| StoryType | Dialogue/Image/Text |
| ImageId | ???????? |
| 背景图片 | ??????????????????????? |
| TextContent | ????????? |
| TriggerTaskOnEnd | ????????? |
| TriggerTaskId | ???????ID |
| AddItemId | ???????? |
| AddItemCount | ???? |
| Remark | ?? |

## DialogueConfig

| ?? | ?? |
|---|---|
| LineId | ???ID |
| StoryId | ????ID |
| NextLineId | ???ID???????? idA|idB |
| SpeakerCharacterId | ??????ID |
| Content | ?????????? ??A|??B |
| Position | 0??1? |
| IsChoice | ?????? |
| RequiredItemIds | ?????????? | |
| RequiredItemCounts | ?????????? | |
| ConsumeItems | ?????????? | |
| AddItemIds | ?????????? | |
| AddItemCounts | ?????????? | |
| 演出 | ??????????????????????骷髅_演出点位起始??骷髅_上升300回初始位?? |
| Remark | ?? |

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

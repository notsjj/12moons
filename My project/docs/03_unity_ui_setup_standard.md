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
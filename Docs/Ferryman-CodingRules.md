# Ferryman 过河玩法 · 代码规则与编写规范

本文件是 `Assets/GameScript/Hotfix/Ferryman/**` 与相关 UI 的长期约定。新增代码前先读一遍。

## 1. 分层与依赖

```
View  →  Core  →  Data
 │         │        └─ Cfg* 配表 / board.json / DataFerryProfile 存档
 │         └─ 纯逻辑：会话、规则、计分、流程命令
 └─ Unity 表现：SpriteRenderer、uGUI、相机、动画
```

- `Core/**` **禁止** `using UnityEngine`、`using UnityEngine.UI`，只允许纯 C# 与纯数学。
  `Assets/GameScript/Editor/Validation/FerryLayeringValidator.cs` 会在编译前扫描并报错。
- `View/**` 可以引用 `UnityEngine`，但**不允许**直接改逻辑层状态，只能读 `FerryGameSession`。
- `UI/**` 只读数据 + 发意图 + 订阅事件，所有玩法操作必须走 `FerryGameCommands`。
- 启动层与业务层同属 `Script` 程序集；窗口与数据分别由框架的 `WindowManager` / `DataManager` 管理，
  新增窗口打 `[Window]` 特性、新增数据类打 `[Data]` 特性即可被自动收集，不需要手工登记表。
  分层靠目录约定 + 校验脚本保障。

## 2. 窗口

沿用框架五步，缺一不可：

1. `Assets/GameRes/UIPanel/<窗口类名>.prefab`：根挂 `Canvas + GraphicRaycaster + UIManifest`；
2. 窗口类继承 `WindowBase`，类名必须与 prefab 同名（YooAsset 按类名拼路径）；
3. 类上打 `[Window((int)EWindowLayer.Xxx, fullScreen)]`，`WindowManager` 反射收集，不需要登记表；
4. 控件在 `OnCreate()` 用 `FerryUIBuilder` 生成（prefab 只留结构，避免节点路径漂移）；
5. `UIManifest` 自动刷新，收集器已包含该目录。

层级：Menu / Select / Result / Rules / Help / Options = `Panel`，Hud = `HUD`，教程气泡 = `Guide`，
`UILoading` 保留。窗口销毁时用 `AddEventListener` 或 `RemoveAllListener` 管理监听，禁止裸 `+=`。

**相机**：`UIRoot.prefab` 的 UICamera 是 `ScreenSpaceCamera`，Canvas `planeDistance = 100`，
所以相机 `far clip plane` 必须大于 100；棋盘相机 `depth` 必须小于 UI 相机，否则 uGUI 被整屏棋盘盖住。

## 3. 坐标

- 关卡数据、教程气泡、场景物件一律是 **1000×562 设计稿坐标，Y 向下**。
- `FerryBoardAnchor.ToWorld(x, y) = (x, DesignHeight - y)`，1 设计像素 = 1 世界单位。
- 2D 棋盘相机 `orthographicSize = DesignHeight * 0.5`，即舞台正好铺满屏幕竖向。
- 贴图锚点是**左上角**，精灵 pivot 是**底边中点**，换算放在 `FerryBoardView.ScenePosition()`。
- 相机横移按场景贴图包围盒（内容范围）夹取，不按 `canvasWidth`，否则多码头关卡会露出空背景。

## 4. 站位与表现契约

以 `D:\Ferryman_setup\FerryMan_Unity\Assets\Scripts\View\BoardView.cs` 为业务契约：

- 码头站位表**只算一次**，棋子离开后其余棋子不挪位（`FerryDockSlots`）；
- 中间码头按船停靠侧排半区，终点码头首站右移 24、列距 56；
- 船底贴水面（吃水 18%），甲板线在船高 35% 处；船夫 `-26 / +18`；
- 单人船乘客整体右移 40，乘客列距 46；
- 动物走路 `speed(格/秒) × 30px`，时长夹在 0.3~2 秒；划船固定 0.75 秒；
- 水面浪花帧**顶边**贴水面 +2px；按 `waterType` 选 `.spt` 行；多河道就近匹配水面；
- 场景动画 `fps × 0.5`，动物动画 `fps × 2`，船夫 `fps × 1`；
- 排序量级：场景 `renderOrder*1000 + order`，船 3000，船夫 3001，棋子 4000+索引，浮字 5000。

## 5. 配表与数据

- 7 张表在 `表格/*.xlsx`，5 行表头（类型 / 小驼峰字段名 / `C` 标记 / 策划描述 / 中文名），
  `id` 列必备，`List<int>` 用 `;` 分隔，文本列用 `language` 走 `LANG`。
- 工程内 Cfg 采用 `ConfigTab + AddElement(...)` 硬编码容器（**不是** `Tool/ExcelTools` 的二进制版）。
  改结构时同步更新 `表格/*.xlsx` 与 `Config/AutoGenerateConfig/Cfg*.cs`，这是已知技术债。
- 大图帧矩形 / `sceneAnims` / `hero` 走 `Assets/GameRes/Ferryman/Data/board.json`，
  由 YooAsset 以 `TextAsset` 加载；`sceneAnims.row` 对应原版 `.spt` 行号，水面按 `waterType` 选行。
- 配表文本一律走 `LocalizationManager.Get(key)`，禁止在代码里硬编码 UI 文案。

## 6. 资源与日志

**目录约定**（旧 demo 的 `Assets/GameArt`、`GameRes/Audio|Entity|UITexture` 已全部删除，不要再往里放东西）：

| 位置 | 内容 |
|---|---|
| `Assets/GameRes/Ferryman/Entity/` | 棋子、场景、船、UI 散图（png，文件名被 `board.json` 引用，改名要同步改 json） |
| `Assets/GameRes/Ferryman/Audio/` | 玩法音效（wav），`FerryAudio` 以 `Ferryman/Audio/{name}` 播放 |
| `Assets/GameRes/Ferryman/Data/board.json` | 大图帧矩形、`sceneAnims`、`hero` |
| `Assets/GameRes/Ferryman/UI/Loading/` | 加载界面进度帧源图（被 `UIAtlas/UILoading.spriteatlas` 收集） |
| `Assets/GameRes/UIAtlas/UILoading.spriteatlas` | 目前唯一保留的图集（加载界面） |
| `Assets/GameRes/UIPanel/` | 窗口预制体，命名必须与窗口类名一致 |
| `Assets/GameRes/Scene/Launcher.unity` | 唯一启动场景 |
| `Assets/GameRes/Config/Localization.csv` | 运行时多语言表 |

- YooAsset 全路径寻址：`Assets/GameRes/Ferryman/Entity/...`（不带扩展名）。
  单关 sheet 按需加载，切关 `handle.Release()`；切帧结果缓存 `Sprite[]`，禁止逐帧 `Sprite.Create`。
- 只用 `HotfixLogger`，禁止 `Debug.Log`；关卡加载 / 规则求值失败必须带关卡 id 与资源 key。
- 存档走 `DataFerryProfile`（`DataManager.Instance.GetData<DataFerryProfile>()`），
  进度/解锁/最高分/总分集中在这里，不另建 PlayerPrefs key。

## 7. 启动状态链

`Prepare → Menu → Select → Playing`，`HotfixStatePlaying.Execute()` 是唯一 Update 根，
驱动 `FerryGameFlow.Tick()` 与 `FerryBoardView.Tick()`；视图不写自己的 `Update`。
`UIWindow.OnUpdate()` 只做刷新文本，不做玩法推进。

改动 `EHotfixStateType` / `HotfixMain.Start` 的状态注册 / `[Window]` 层级 / `[Data]` 数据类 /
`HotfixEventMessageDefine` / `AssetBundleCollectorSetting.asset` 时必须同批改完。

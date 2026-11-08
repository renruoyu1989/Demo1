# Demo1

演示程序是一款使用[MotionFramework](https://github.com/gmhevinci/MotionFramework)框架开发的**单机** RPG 游戏，所有游戏业务逻辑（窗口、战斗、数据）都作为普通程序集直接编译进工程。

> 本工程是纯原生单机工程：所有代码随主包编译，运行时不加载外部程序集；没有网络模块与 Patch/CDN 补丁流程。

**注意事项**

1. 框架已内置在 `Assets/MotionFramework`（裁剪版），**请不要再解压 `MotionFramework.zip`**，否则会覆盖成旧版框架导致编译报错。
2. `GameLauncher.PlayMode` 控制资源运行模式：编辑器下强制 `EditorSimulateMode`（直接读工程内资源），打包后走 `OfflinePlayMode`（包体内置资源）。

**程序集结构**

| 程序集 | 作用 |
|---|---|
| `MotionFramework` | 引擎层 + 实际使用的模块（资源、场景、音频、对象池、事件、控制台） |
| `Script.ThirdParty` | 第三方库（Mono.Xml） |
| `Script` | 启动层、通用系统与全部游戏业务逻辑（启动器、存档、多语言、UGUI、状态机、UI 窗口、过河玩法） |
| `MotionFramework.Editor` / `Script.Editor` | 编辑器工具（AssetBundle 打包、资源导入、UI 工具） |

**存档系统**

- `SaveManager`（`Script/Runtime/Save`）：JSON 存档，原子写入 + `.bak` 备份 + 版本号校验，落盘于 `Application.persistentDataPath/Save/`
- `DataManager`（`Hotfix/Data`）：聚合各 `DataBase` 的存档片段，提供 `SaveAll / LoadAll / DeleteSave / ResetAll`
- 自动存档：切后台、失去焦点、退出游戏时自动触发
- 编辑器调试：`F5` 手动存档，`F9` 模拟获得奖励

**多语言系统**

- 数据表：`Assets/Works/Resource/Localization/Localization.csv`（一行一个 Key，三列语言：简体中文 / 繁体中文 / English）
- `LocalizationManager`：运行时切换语言、系统语言推断、`PlayerPrefs` 持久化、切换后自动刷新 UI
- UI 组件：`UILocalizeText`、`UILocalizeDropdown` 自动跟随语言变化
- 游戏内切换入口：设置窗口里的语言按钮（三语循环切换）

**配置表**

导表工具使用的是[闪电导表工具](https://github.com/gmhevinci/FlashExcel)，源表在 `表格/` 目录。

![image](https://github.com/gmhevinci/Demo1/raw/master/Docs/Image/img3.png)

**游戏截图**

![image](https://github.com/gmhevinci/Demo1/raw/master/Docs/Image/img1.png)
![image](https://github.com/gmhevinci/Demo1/raw/master/Docs/Image/img2.png)

**声明**

演示程序使用的美术资源仅供参考学习，请不要用于商业化项目。

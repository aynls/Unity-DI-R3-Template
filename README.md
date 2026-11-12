# Unity DI-R3 Template

面向中小型独立游戏的 Unity 6 工程模板，采用 URP 2D。示例场景把首次启动、语言选择、主菜单和设置页串在一起，可以从这条流程开始接入自己的玩法。

## 运行示例

1. 安装 Git 和 Unity `6000.3.5f2`，克隆仓库。编辑器版本以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准。

   ```bash
   git clone https://github.com/aynls/Unity-DI-R3-Template.git
   ```

2. 在 Unity Hub 中打开克隆后的项目目录，等待依赖下载、资源导入和脚本编译完成。
3. 打开 [SampleScene](Assets/Scenes/SampleScene.unity)，进入 Play Mode。首次启动可以选择简体中文、英语或日语。确认语言后，按任意键打开主菜单，再进入设置页调整分辨率、全屏模式、音乐和音效音量。再次启动时会恢复已保存的语言和设置。

## 模板提供的模块

| 模块 | 用途 |
| --- | --- |
| 启动与依赖注入 | `ProjectLifetimeScope` 用 VContainer 注册服务和场景组件，`GameFlowController` 用 UniTask 编排可取消的启动流程。 |
| 页面导航 | uGUI 页面通过 Addressables 加载并由 VContainer 实例化。导航栈支持 Push、Pop、Replace 和 Clear，页面过渡使用 DOTween。 |
| 设置与存档 | 设置通过 R3 发布已保存的状态变化。设置和游戏存档分别写入本地 JSON，支持原子写入与备份恢复。玩法可通过 `IGameSaveService` 接入槽位存档。 |
| 本地化 | Unity Localization 管理语言、文本和资源表，启动时应用已保存的语言。 |
| 音频 | BGM 支持淡入淡出，SFX 支持重叠播放，AudioMixer 独立控制两路音量。示例附带点击音效。[音频接入指南](Assets/Doc/GameSound.md) |

## 接入自己的游戏

在 [ProjectLifetimeScope](Assets/Scripts/Architecture/DependencyInjection/ProjectLifetimeScope.cs) 注册应用服务，通过 [GameFlowController](Assets/Scripts/Architecture/GameFlow/GameFlowController.cs) 接入启动流程。从 [页面脚本](Assets/Scripts/UI/Page) 和 [示例 Prefab](Assets/Prefab) 可以查看页面如何绑定控件、响应输入和切换界面。

## 构建 Windows 版本

在 Unity 的 **Build Profiles** 中切换到 Windows x64，然后运行 **Tools > Template > Build > Packed Addressables + Windows Player**。构建时会检查 UIBinder 绑定、Audio Catalog 和生成的资源常量，产物位于 `Builds/Windows/DI-R3-Template.exe`。

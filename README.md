# Unity DI-R3 Template

A Unity 6 (URP 2D) starter for small indie games — DI, async startup, persistence, localization and audio already wired, menus built with UI Toolkit. Clone it and go straight to gameplay.

## 技术栈

Unity `6000.3.x` · URP 2D · VContainer · R3 · UniTask · MessagePipe · DOTween · Addressables · Localization · Input System · Newtonsoft.Json

## 示例流程

打开 `Assets/Scenes/Game.unity` 进入 Play Mode：首次启动选择语言（简中/EN/日語）→ 主菜单 → 设置页调分辨率、全屏、音乐与音效音量。退出后再启动会恢复已保存的语言和设置。

## 模板提供什么

| 模块 | 说明 |
| --- | --- |
| 启动与依赖注入 | `ProjectLifetimeScope` 组合根注册服务，`GameFlowController` 用 UniTask 编排可取消启动。 |
| 页面导航 | UI Toolkit 页面（UXML/USS + 纯 C# 控制器），导航栈支持 Push/Pop/Replace/Clear，转场串行排队、失败回滚。 |
| 设置与存档 | R3 发布状态变化，JSON 原子写入 + 备份恢复；`IGameSaveService` 留给玩法存档。 |
| 本地化 | Unity Localization 字符串/资源表，UXML 直接绑定表条目。 |
| 音频 | BGM 淡入淡出、SFX 重叠播放、AudioMixer 双路音量。见 [音频接入指南](Assets/Doc/GameSound.md)。 |

## 开始

1. `git clone https://github.com/aynls/Unity-DI-R3-Template.git`
2. Unity Hub 打开目录（编辑器版本以 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) 为准），等待导入与编译。
3. 在 [ProjectLifetimeScope](Assets/Scripts/Architecture/DependencyInjection/ProjectLifetimeScope.cs) 注册你的服务，从 [页面控制器](Assets/Scripts/UI/Page) 与 [Assets/UI](Assets/UI) 的 UXML/USS 开始搭界面。

## 构建 Windows 版

Build Profiles 切到 Windows x64，运行 **Tools > Template > Build > Packed Addressables + Windows Player**，产物在 `Builds/Windows/`。构建会校验 UIRoot 页面模板、Audio Catalog 与生成的资源常量。

# Unity DI-R3 Template

面向中小型独立游戏的 Unity 6 工程底座。

每次开新项目，总要重搭一遍启动编排、首次语言选择、主菜单、设置面板、双路音频与本地存档；常见模板要么退化成满屏 `Instance` 单例，要么捆绑一整套侵入式的玩法框架。本仓库只把**进入玩法之前的基础设施**做干净——克隆下来即可跑通从冷启动到主菜单的完整闭环，随后直接写你的游戏逻辑。

## 设计亮点

- **单一组合根，零静态单例**
  全工程由场景中的 [`ProjectLifetimeScope`](Assets/Scripts/Architecture/DependencyInjection/ProjectLifetimeScope.cs) 统一装配，模块间只通过接口与构造函数协作，不设全局 `Singleton` 或 Service Locator，顺着构造函数就能读清整张依赖图。

- **UI Toolkit 视图 + 纯 C# 页面栈**
  不再维护挂满序列化引用的 uGUI 页面 Prefab。布局、过渡与多语言文本绑定留在 [`Assets/UI`](Assets/UI) 的 UXML / USS 中，页面控制器只是由容器按需解析的普通 C# 类；[`PageNavigator`](Assets/Scripts/UI/Navigation/PageNavigator.cs) 将 Push / Pop / Replace / Clear 串行排队，转场失败自动回滚至上一页，离开页面时统一取消异步任务并解绑事件与订阅。

- **先落盘、后广播的不可变状态**
  设置与存档服务对外只暴露 `record` 不可变快照 `Current` 与 `Changes` 变更流。写入采用 `.tmp` 原子替换并保留 `.bak` 备份，启动时主文件损坏会自动从备份恢复；**先落盘成功，再更新内存快照并推送通知**，界面不会出现未持久化的假状态。

- **显式结果契约与清晰的所有权**
  全链路使用 `UniTask` 与 `CancellationToken` 传播取消，零协程、无静默 `catch`，可恢复失败统一返回明确的 `Result / Status`。音频层通过 [`IMusicPlayer` / `ISfxPlayer`](Assets/Scripts/Architecture/GameSound/AudioContracts.cs) 隔离 `AudioSource` 与 Addressables 句柄，自动合并同一资源的并发加载，支持 BGM 淡入淡出与最新指令抢占。

- **强类型资源常量与构建期检查**
  Addressables 地址与 `AudioCatalog` 条目一键生成为 C# 常量，告别手写字符串；Player 构建时自动校验场景 `UIRoot` 页面模板、音频目录与生成代码是否同步，把断引用拦在打包前。

## 技术选型与分工

每个依赖只负责一块明确的边界，职责互不重叠：

| 领域 | 选型与职责 |
| --- | --- |
| 装配与消息 | **VContainer** 管理组合根与对象生命周期；**MessagePipe** 负责跨模块瞬时消息，替代静态事件中心。 |
| 状态与异步 | **R3** 承载有当前值的响应式状态流（`Current` + `Changes`）；**UniTask** 串联启动、转场与异步加载，由作用域 Token 统一收口。 |
| 界面、输入与插值 | **UI Toolkit** 负责 UXML 声明式布局、USS 过渡与本地化直绑；**Input System** 处理输入监听；**DOTween** 处理音频渐变等连续插值（已开启 UI Toolkit 扩展）。 |
| 资源与多语言 | **Addressables** 管理按需加载与引用计数；**Unity Localization** 管理简中 / 英 / 日文本与资源表，配套常用字集烘焙的 TextCore 字体。 |
| 本地持久化 | **Newtonsoft.Json** 配合版本化 Schema 校验与原子文件替换，分别管理用户设置与多槽位存档。 |

## 目录结构

```text
Assets/
├── Scenes/Game.unity              # 启动场景（组合根、UIRoot、音频输出）
├── UI/                            # PanelSettings、TextCore 字体、菜单 UXML 与 USS
├── Scripts/
│   ├── Architecture/
│   │   ├── DependencyInjection/   # ProjectLifetimeScope 组合根
│   │   ├── GameFlow/              # GameFlowController 可取消启动主链
│   │   ├── Data/                  # Settings、GameSave 与原子文件持久化
│   │   ├── GameSound/             # BGM / SFX 播放接口与 Addressables 句柄池
│   │   └── Language/              # 多语言服务与设置联动
│   ├── UI/                        # UIRoot、PageNavigator / PageStack、页面控制器
│   └── Generated/                 # 自动生成的 Addressables 与音频常量
├── Settings/Localization/         # Locale 与字符串 / 资源表
└── Editor/                        # 常量生成工具与构建期校验器
```

## 快速体验

1. 克隆仓库并用 Unity Hub 打开（编辑器版本 `6000.3.5f2`，以 [`ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt) 为准）：

   ```bash
   git clone https://github.com/aynls/Unity-DI-R3-Template.git
   ```

2. 打开 [`Assets/Scenes/Game.unity`](Assets/Scenes/Game.unity) 进入 Play Mode：
   - **首次启动**进入语言确认页（简体中文 / English / 日本語），切换即时生效并写入本地设置；
   - 确认后切入主菜单，按任意键展开菜单并触发点击音效；
   - 打开设置页可调整分辨率、全屏模式与音乐 / 音效音量，再次启动自动恢复。

## 接入你的游戏

- **接入玩法入口**：在 [`ProjectLifetimeScope`](Assets/Scripts/Architecture/DependencyInjection/ProjectLifetimeScope.cs) 注册你的领域服务，从 [`MainScenePage`](Assets/Scripts/UI/Page/MainScenePage.cs) 预留的开始按钮切入玩法流程。
- **新增 UI 页面**：在 `Assets/UI` 编写 UXML / USS，继承 [`BasePage`](Assets/Scripts/UI/Page/BasePage.cs) 实现控制器，在组合根注册为 `Transient` 并绑定到场景 `UIRoot`。
- **扩展槽位存档**：在 [`GameDataRuntime`](Assets/Scripts/Architecture/Data/GameSave/GameSaveContracts.cs) 及仓储映射中添加字段，通过 `IGameSaveService` 读写槽位。
- **添加音乐与音效**：将音频标记为 Addressable 后在 `AudioCatalog` 生成强类型索引，详见 [音频接入指南](Assets/Doc/GameSound.md)。

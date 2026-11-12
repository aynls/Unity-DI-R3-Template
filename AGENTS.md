# DI-R3 Template 协作指南

DI-R3 Template 是面向中小型独立游戏的 Unity 模板。第一方代码全部是基础设施：`Architecture`（DI、启动流程、数据、音频、语言）与 `UI`（导航、页面）。具体玩法规则不写进这两层。

以当前代码、场景和 ProjectSettings 为准，修改前先读目标模块及其调用方。

## 基线

- Unity `6000.3.5f2`，URP 2D；VContainer、R3、UniTask、MessagePipe、DOTween、Addressables、Localization、Input System、Newtonsoft.Json。
- 第一方代码没有 asmdef：`Assets/Scripts` 进 `Assembly-CSharp`，`Assets/Editor` 进编辑器程序集。
- `Assets/Plugins`、`Assets/NuGet`、`Assets/Packages`、`Assets/TextMesh Pro` 是第三方，不修改。运行时代码不依赖 Odin。
- 运行时命名空间：`Architecture`、`Architecture.Data.*`、`Architecture.Audio`（目录名是 `GameSound`）、`UI`、`Generated`；编辑器代码用 `Template.Editor.*`。

## 运行时主链

`Game.unity` 中的 `ProjectLifetimeScope` 是唯一组合根，长生命周期服务都在这里显式注册。

`GameFlowController.Start()` 是入口：初始化设置与语言，应用 Locale、Display、Audio，再经 `IPageNavigator` 进入首页。首次启动进 `LanguagePage`，确认后 Replace 为 `MainScenePage`；`SettingsPage` 在其上 Push/Pop。启动任务绑定销毁 token，取消不算启动失败。

## 架构规则

- **访问修饰符**：第一方 C# 不使用 `internal`，统一显式 `public`。
- **依赖**：暴露接口，构造函数注入；场景对象用 `RegisterComponent`。不引入 Service Locator 或全局单例；跨模块直接调用服务接口，发布订阅走 MessagePipe（`RegisterMessagePipe` 已在组合根就绪）。
- **状态**：`ISettingsService`、`IGameSaveService` 是各自状态的唯一所有者，对外只给不可变 snapshot 和受控命令。初始值读 `Current`，后续变化订阅 `Changes`。可恢复的失败返回明确的 Result/Status，不用静默 `catch`，不堆砌无意义的 try-catch。
- **持久化**：Service → Repository（schema 校验、JSON 映射）→ `IFileStore`（原子替换加 `.bak`）。设置修改先落盘成功，再替换 `Current`。模板是基线，不为旧数据写兼容逻辑。
- **UI**：运行时页面用 UI Toolkit。`UIRoot` 持有 `UIDocument` 和各页 UXML；布局、样式、过渡写在 `Assets/UI` 的 UXML 与 USS，控件 `name` 页内唯一。页面控制器继承 `BasePage`，由 `IPageNavigator` 经 VContainer 创建并挂载视觉树。新增页面：在 `ProjectLifetimeScope` 注册为 `Transient`，在 `UIRoot` 绑定其 UXML。转场串行排队，失败回滚到旧页；Replace 时旧页须在 `OnPause` 完成隐藏。离开页面时取消异步任务、解绑控件事件并释放订阅。
- **音频**：调用方只依赖 `IMusicPlayer`、`ISfxPlayer`、`IAudioLevelsControl`，不直接操作 `AudioSource`。细节见 `Assets/Doc/GameSound.md`。
- **语言**：当前为 `en`、`zh-Hans`、`ja`。UI 文案优先在 UXML 中绑定 Localization 表，字体使用 TextCore。加语言要同步 `GameLanguageType`、Localization 表、选择 UI 和面板字体。
- **生成代码**：`Assets/Scripts/Generated` 不手改。修改 Addressable 后运行 **Tools > Template > Code Generation > Generate Addressable Keys**；新增音频后在 `AudioCatalog` Inspector 点 **Auto Generate Index**。Player Build 会检查生成文件和场景的 UI Document、面板及页面引用。
- **生命周期**：异步用 `UniTask`，禁止协程。`async void` 只用于 Unity 消息；`.Forget()` 只用于无法等待的事件边界，且异常要有日志。R3 订阅、Tween、Addressables handle 必须有所有者：MonoBehaviour 用 `.AddTo(this)`，纯 C# 服务用 `DisposableBag` 并在 `Dispose` 释放。
- **编辑器代码**：UnityEditor API 只放在 `Assets/Editor`，或完整包在 `#if UNITY_EDITOR` 内。
- **克制**：不为假想需求加抽象，新接口要对应真实的替换点或跨模块边界。日志带模块名、操作和关键 ID。

## 资源与验证

- 移动或重命名资源时保留 `.meta` 与 GUID。场景、Prefab、ScriptableObject 和 Addressables 配置通过 Unity Editor 修改，不手工大改 YAML。
- 改了 `ProjectLifetimeScope` 或 `UIRoot` 的序列化字段后，检查 `Game` 场景的 Inspector 引用。
- 按改动范围使用 Unity Editor 或 batchmode 验证编译，并按需执行 **Validate Generated Code** 或 Player 构建。Play Mode 与实际游玩验证由用户执行。Unity MCP 默认仅在 Edit Mode 下做只读检查。
- 项目以编译、编辑器校验、构建和用户手动验证为主。不新增第一方测试、测试场景或程序集，除非用户明确要求；不自行执行可能产生副作用的验证步骤。

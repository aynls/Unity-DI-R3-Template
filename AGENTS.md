# DI-R3 Template 协作指南

## 项目入口

Unity 版本以 `ProjectSettings/ProjectVersion.txt` 为准。`Assets/Scenes/SampleScene.unity` 中的 `ProjectLifetimeScope` 注册场景组件与服务，`GameFlowController` 编排启动。改动前检查工作区、目标模块及调用方，以当前代码、场景和配置为准，保留用户已有改动。

## 实现约定

- 在 `ProjectLifetimeScope` 显式注册服务，优先通过接口和构造函数注入。保持它作为默认组合根，避免引入全局 EventBus、Service Locator 或额外的全局单例。
- 由设置和存档服务持有并修改各自的状态。写入成功后再更新 `Current` 并发布 R3 变化。调用方显式读取初始快照。修改持久字段时同步默认值、模型和仓储映射，检查现有数据的读取行为。
- 页面由 `IPageNavigator` 导航，通过 Addressables 加载 Prefab 并由 VContainer 实例化。保留可取消的生命周期、失败回滚和资源释放。页面 Prefab 保持 UIBinder 命名约定及唯一绑定 ID。
- 异步接口优先使用 UniTask，传递与操作生命周期一致的取消令牌。`.Forget()` 留给无法等待的 Unity 事件边界，并处理异常。订阅、Tween 和 Addressables 句柄由各自的所有者释放。
- 第一方运行时代码使用 `Assembly-CSharp`，保持对 Odin 的独立性。编辑器逻辑放在 `Assets/Editor` 或 `UNITY_EDITOR` 条件编译块内。任务涉及第三方依赖时再改动对应目录。

## 资源与验证

- 移动 Unity 资源时保留 `.meta` 和 GUID。场景、Prefab 和资源配置优先通过 Unity Editor 修改。
- 更新 Addressables 或音频后，通过 **Tools > Template > Code Generation > Generate Addressable Keys** 或 `AudioCatalog` Inspector 的 **Auto Generate Index** 重新生成对应常量。
- 按改动范围使用 Unity Editor 或 batchmode 验证编译，并按需执行 **Validate UIBinders**、**Validate Generated Code** 或 Player 构建。Play Mode 与实际游玩验证由用户执行。Unity MCP 默认仅在 Edit Mode 下做只读检查。
- 项目以 Unity 编译、编辑器校验、构建和用户手动验证为主。新增第一方测试、测试场景或程序集前遵循用户明确给出的范围。

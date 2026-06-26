# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概览

胡闹厨房（Cooking Simulator）—— 一款使用 Unity 2022.3.62f3c1 开发的 2D 像素风做饭游戏。玩家登录后选择厨神模式，按菜谱做菜，接受 AI 或本地评价，并将菜品保存到食单。核心流程链路：

```
登录 → 模式选择 → 菜谱选择 → 做菜 → 评价 → 保存菜品 → 食单
```

## 仓库规则

完整的操作规范见 `AGENTS.md`。关键硬性约束：

- **MVP 优先**：MVP 未完成前，禁止添加非 MVP 功能、禁止设计扩展架构、禁止优化性能或重构。只允许实现 `docs/mvp.md` 中定义的内容。
- **分支策略**：禁止直接提交到 `main`。在 `dev` 分支基础上创建 `feature/*` 分支进行开发，通过 PR 合并到 `dev`。
- **范围纪律**：如果需求不属于当前任务，必须明确指出并回到当前任务范围。

## 构建与验证命令

所有 Unity 命令需要 Unity Editor 路径 `D:\unity\Editor\Unity.exe`，且通常需要提权执行。如果 Unity Editor 已打开，batchmode 会失败——先提醒用户关闭。

```powershell
# 仅验证 C# 编译（不重建场景）：
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -logFile "D:\CookingSimulator\unity-verify-<task>.log"

# 从代码重建 MVP 场景（MvpSceneBuilder.BuildScene）：
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildScene -logFile "D:\CookingSimulator\unity-rebuild-<task>.log"

# 构建 Windows 玩家包：
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildWindowsPlayer -logFile "D:\CookingSimulator\unity-build-windows-<task>.log"
```

**验证模式**：修改 MonoBehaviour 序列化字段、UI 绑定或场景生成器代码后，必须重建场景。新增 `.cs` 脚本后，必须通过 Unity 导入生成 `.meta` 文件。检查日志中是否有 `error CS`、`Scripts have compiler errors` 或 `Compiler errors`。Unity 退出码为 0 且无 C# 编译错误即为通过。

## 测试 AI 品鉴接口

```powershell
# Python CLI 工具（无需 Unity）：
python tools/test_ai_review_api.py

# Unity Editor 菜单项：
# "Cooking Simulator > Test AI Review API"（调用 AIReviewTester.TestAIReviewApi）
```

需要在项目根目录配置 `ai_review.local.json`（已被 gitignore），结构如下：
```json
{
  "providers": [
    { "name": "openai", "baseUrl": "https://api.openai.com/v1", "apiKey": "...", "model": "gpt-4o" }
  ]
}
```

## 架构

### 命名空间布局

| 命名空间 | 目录 | 职责 |
|---|---|---|
| `CookingSimulator.Core` | `Assets/Scripts/Core/` | 核心游戏循环、页面流转、状态机 |
| `CookingSimulator.Models` | `Assets/Scripts/Models/` | 可序列化数据类型（不含 Unity 逻辑） |
| `CookingSimulator.Services` | `Assets/Scripts/Services/` | 文件读写、菜谱加载、AI HTTP 调用、评价逻辑 |
| `CookingSimulator.UI` | `Assets/Scripts/UI/` | MonoBehaviour 视图、用户输入、界面展示 |
| `CookingSimulator.Editor` | `Assets/Scripts/Editor/` | 场景生成、构建管线、编辑器工具 |

### 核心循环（`GameManager`）

`GameManager` 是唯一的 orchestrator MonoBehaviour。它持有所有 services 和 UI views 的序列化引用。页面切换遵循严格模式：当前 view 调用回调，`GameManager` 隐藏旧 view、更新状态、显示下一个 view。UI 类之间互不引用——所有流程都经过 `GameManager`。

### 状态机（`DishState`）

```
Raw → Cut → Cooking → Seasoned → Done
```

`GameManager.TryApplyAction()` 校验状态转换。只有合法的转换序列才被接受；非法操作提示"当前不能这样做"。

### 数据模型（均为 `[Serializable]`，纯 C# 对象）

- **UserData** — userId、username、reputation、时间戳。保存至 `Saves/Users/{id}.json`。
- **RecipeData** — recipeId、name、ingredients、seasonings、有序的 RecipeStep[]。从 `StreamingAssets/Recipes/*.json` 加载。
- **CookingLog** — 完整操作日志：logId、userId、dishId、recipeId、List\<ActionRecord\>、finalState。保存至 `Saves/Logs/{logId}.json`。
- **ActionRecord** — 单次操作：动作名、操作对象、elapsedSeconds、stage、stateBefore→stateAfter。
- **DishData** — 已保存菜品：dishId、userId、name、price、score、logPath、reviewId、reviewText。保存至 `Saves/Dishes/{dishId}.json`。
- **ReviewData** — reviewId、dishId、score（0-100）、summary、suggestion、reputationDelta。保存至 `Saves/Reviews/{reviewId}.json`。

### Services 层

- **SaveManager**（单例 MonoBehaviour）—— 唯一的文件读写入口。UI 绝不直接操作文件。管理 `Application.persistentDataPath` 下的 `Saves/Users/`、`Saves/Logs/`、`Saves/Dishes/`、`Saves/Reviews/` 目录。
- **RecipeManager** —— 从 `StreamingAssets/Recipes/` 加载并反序列化菜谱 JSON。
- **LogManager** —— 创建做菜日志会话，通过 `Time.time` 记录时间戳，记录状态转换。
- **ReviewManager** —— 生成基于本地规则的评价（步骤完成度评分）和降级用的"老八"评价。纯逻辑，无 I/O。
- **AIReviewService** —— 基于协程的异步 AI 评价，调用 OpenAI 兼容的 chat completions API。支持多 provider 故障转移。从 LLM 响应中解析 JSON。任何失败（配置缺失、网络错误、解析失败）都会降级到本地评价。

### UI Views

所有 UI 类均为普通 MonoBehaviour，通过 `[SerializeField]` 引用 Unity UI 组件（Text、InputField、Button、Image）。它们暴露 `Show(...)` 方法接收回调 Action，通过 `gameObject.SetActive(false)` 隐藏自身。核心模式：views 只接受 Action 回调，绝不直接引用其他 view 或 service。

### 场景生成

`MvpSceneBuilder` 是一个 Editor 脚本，**通过代码构建整个 MVP 场景**。它创建 Canvas（1280×720 参考分辨率，ScreenSpaceOverlay 模式）、Camera、EventSystem、所有 service GameObject、所有 UI 面板，以及带全部序列化字段绑定的 GameManager。不存在手工编辑的场景——场景始终从代码重新生成。`Assets/prefab/UI/` 中的 prefab 在可用时被实例化；其余面板由基本的 Image/Text/Button 组件原始构建。

### 文件布局（重要路径）

```
Assets/Scenes/MVP.unity                          ← 唯一的场景
Assets/Scripts/Core/GameManager.cs                ← 核心编排器
Assets/Scripts/Models/*.cs                        ← 数据类型
Assets/Scripts/Services/SaveManager.cs            ← 文件读写入口
Assets/Scripts/Services/AIReviewService.cs        ← AI 集成
Assets/Scripts/UI/*.cs                            ← 所有视图
Assets/Scripts/Editor/MvpSceneBuilder.cs          ← 场景生成
Assets/StreamingAssets/Recipes/tomato_egg.json    ← 默认菜谱
Assets/StreamingAssets/NPCs/ai_laoba.md           ← AI 品鉴者人格
Assets/prefab/UI/*.prefab                         ← 手工制作的 UI prefab
Assets/kitchen/                                   ← 厨房精灵素材 & 切图
docs/mvp.md                                       ← MVP 范围定义
docs/ui_design.md                                 ← UI 重构方向
docs/项目开发依据.md                                ← 完整项目规格书
docs/todolist.md                                  ← 当前任务跟踪
tools/test_ai_review_api.py                       ← AI API 连接测试工具
```

### 关键设计规则

1. **UI 不接触文件** —— 所有持久化操作必须经过 `SaveManager`。
2. **Services 是被动的** —— 它们不调用 UI；所有对接由 GameManager 完成。
3. **AI 是增强体验，不是依赖** —— AI 失败时必须降级到本地评价。
4. **所有存档数据为 JSON** —— 使用 `JsonUtility.ToJson`/`FromJson`。
5. **场景由代码生成** —— 修改场景布局时改 `MvpSceneBuilder`。
6. **MonoBehaviour 序列化字段** —— 为 UI 或 service 类新增字段时，场景生成器中的 `Assign()` 调用必须绑定该字段，或者 prefab 中必须包含该字段。

# 胡闹厨房 / Cooking Simulator

2D 像素风做饭游戏 —— 选菜谱、做菜、AI 品鉴、保存食单。

## 核心链路

```
登录 → 模式选择 → 菜谱选择 → 做菜 → 评价 → 保存菜品 → 食单
```

## 技术栈

| 类别 | 选型 |
|---|---|
| 引擎 | Unity 2022.3.62f3c1 |
| 语言 | C# |
| 测试 | NUnit (Unity Test Framework) + EditMode |
| 数据 | JSON（`JsonUtility`） |
| AI 接口 | OpenAI 兼容 chat completions API |

## 项目结构

```
Assets/
├── Scenes/MVP.unity                    # 唯一场景（由代码生成）
├── Scripts/
│   ├── Core/GameManager.cs             # 核心编排器
│   ├── Models/                         # 可序列化数据类型
│   ├── Services/                       # 文件读写、AI 调用、评价
│   ├── UI/                             # MonoBehaviour 视图
│   └── Editor/MvpSceneBuilder.cs       # 场景生成器
├── Tests/EditMode/                     # 纯逻辑层 NUnit 测试（45/45 通过）
├── StreamingAssets/
│   ├── Recipes/                        # 菜谱 JSON
│   └── NPCs/                           # AI 品鉴者人格 Markdown
├── prefab/UI/                          # UI prefab
├── kitchen/                            # 厨房精灵素材
└── materials/                          # UI 素材
docs/
├── mvp.md                              # MVP 范围定义
├── 项目开发依据.md                       # 完整规格书
├── 测试文档.md                          # 测试文档（GB8567-88）
└── 计划任务书.md                         # 任务跟踪
```

## 构建与运行

```powershell
# 编译验证
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -logFile "unity-verify.log"

# 重建场景
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildScene -logFile "unity-rebuild.log"

# 构建 Windows 包
& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildWindowsPlayer -logFile "unity-build.log"
```

## AI 评价配置

在项目根目录创建 `ai_review.local.json`（已被 gitignore）：

```json
{
  "providers": [
    {
      "name": "openai",
      "baseUrl": "https://api.openai.com/v1",
      "apiKey": "sk-xxx",
      "model": "gpt-4o"
    }
  ]
}
```

```powershell
# 测试 AI 接口
python tools/test_ai_review_api.py
```

## 开发规范

- **分支策略**：`main` ← `dev` ← `feature/*`，通过 PR 合并
- **MVP 优先**：范围见 [`docs/mvp.md`](docs/mvp.md)
- **场景由代码生成**：修改布局改 `MvpSceneBuilder.cs`，然后重建场景
- **UI 不接触文件**：持久化统一走 `SaveManager`
- **AI 可降级**：AI 失败时自动回退到本地评价，不阻断流程

## License

Private

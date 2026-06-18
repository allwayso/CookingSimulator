# MVP 开发待办清单

本文档记录当前 Unity MVP 的实现状态。后续继续开发时，应同时阅读 `docs/项目开发依据.md`、`docs/mvp.md` 和本文档。

## 当前分支与基础状态

- 当前开发分支：`feature/mvp-cooking-polish`
- Unity 版本：`2022.3.62f3`
- 已提交基础工程提交：`e2075d5 feat: scaffold Unity MVP foundation`
- 当前 UI 打磨提交：
  - `6eb8987 feat: polish MVP cooking screen`
  - `ff4bbf9 fix: arrange cooking actions horizontally`
  - `e8fac8c fix: shrink cooking action buttons`
- 当前构建与 AI 提交：
  - `2635f95 build: add Windows player build command`
- 当前 MVP 场景：`Assets/Scenes/MVP.unity`
- 默认菜谱：`Assets/StreamingAssets/Recipes/tomato_egg.json`

## 已实现内容

- Unity 工程骨架已建立，包含 `Assets/`、`Packages/`、`ProjectSettings/`。
- `.gitignore` 已补充 Unity 缓存、日志、本地存档、密钥和构建产物忽略规则。
- 已创建 MVP 场景 `MVP.unity`，并加入构建场景列表。
- 已创建基础 UI 流程：
  - 登录页
  - 模式选择页
  - 菜谱选择页
  - 做菜页
  - 评价页
  - 保存菜品页
  - 食单页
- 已实现厨神模式最小闭环。
- 老八模式目前只作为 MVP 未开放占位。
- 已实现用户名登录和本地用户 JSON 存档。
- 已实现基础菜谱加载。
- 已实现做菜按钮操作：
  - 切菜
  - 下锅
  - 加热
  - 加调料
  - 翻炒
  - 出锅
- 已实现基础菜品状态流转：
  - `Raw -> Cut -> Cooking -> Seasoned -> Done`
- 已实现操作日志记录和 JSON 保存。
- 已实现本地规则评价，不依赖真实 AI API。
- 已实现声望变化并保存到用户存档。
- 已实现菜品命名、定价、保存到食单。
- 已实现食单读取和展示。
- 已给做菜页加入基础 2D 厨房视觉：
  - 厨房背景
  - 台面
  - 锅具
  - 番茄、鸡蛋、调料、火焰占位图形
  - 菜品状态颜色块
- 已实现做菜阶段步骤提示随当前状态推进。
- 已将做菜页操作按钮改为横向排列，并缩小按钮宽度，避免超出右侧边界。
- 已增加 Windows `.exe` 构建命令，并成功生成本地 Windows 构建。
- 已接入 AI 老八评价的 MVP 链路：
  - 使用 `ai_review.local.json` 作为本机私有 API 配置。
  - `api.txt` 和本地 AI 配置已加入 `.gitignore` 保护。
  - 已新增 AI 老八口味文档。
  - 食单页菜品支持按钮点击查看老八评价。
  - AI 调用失败时会降级为本地老八评价。
- 已通过人工测试确认核心逻辑能够跑通。

## MVP 中尚未完善的内容

- 目前只有一份默认菜谱，尚未支持多菜谱内容验证。
- 做菜交互仍是按钮驱动，尚未支持食材、锅具、调料的点击或拖拽交互。
- 当前 UI 仍是 MVP 临时界面，已有基础厨房视觉，但尚未进行正式美术设计。
- 当前没有学习 / 挑战模式切换。
- 当前本地评价规则较简单，只基于关键动作完成情况评分。
- 当前 AI API 代码链路已接入，但本机配置实测返回 401 Unauthorized，需要更新有效 API key/baseUrl/model 后复测。
- 当前没有截图保存和菜品图片字段。
- 当前没有 `BURNT` 烧焦状态。
- 当前非法操作只做简单提示，没有详细错误日志。
- 当前没有退出游戏和恢复未完成进度逻辑。
- 当前没有 Windows `.exe` 构建验证。

## 暂不属于 MVP 的内容

以下内容在 `docs/mvp.md` 中明确不纳入当前 MVP，不应在 MVP 完成前实现：

- 老八完整品鉴流程
- NPC 老八自动品鉴
- 排行榜
- 多菜谱批量导入
- 移动端完整适配
- 复杂动画和音效系统
- 多锅具、多食材自由组合
- 账号注册、密码找回、云端同步
- 图片级 AI 评价

## 下一步建议

1. 更新本机 `ai_review.local.json` 为有效 API 配置，并重新运行 AI API 可用性测试。
2. 增加学习 / 挑战模式切换，但只改变提示显示，不扩展复杂玩法。
3. 强化本地评价规则，让评分能反映顺序错误、缺失动作和重复操作。
4. 加入 `BURNT` 状态和简单火候计时。

## 后续开发原则

- 先保证 MVP 闭环稳定，再增加表现层资源。
- UI 可以简陋，但数据结构和存档必须可靠。
- 新功能必须能对应 `docs/mvp.md`，否则视为非当前任务。
- 不要把 Unity 生成的 `Library/`、`Logs/`、`Temp/`、`UserSettings/`、`.sln`、`.csproj` 提交到仓库。

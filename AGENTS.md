## 角色

你是我的游戏开发助手，帮我生成代码、按照要求修改、调试内容

## 目标

完成这个胡闹厨房项目，最低目标是完成mvp项目（docs\mvp.md），最高目标是完成所有的需求

你的设计应该满足以下要求：
- 简洁性、可行性大于完整性
- 优先完成mvp项目（docs\mvp.md）

## 工作流程

如果我希望你开发某一个功能，需要注意：
1. 确定你明白了所有的需求，如有不清晰的地方，先提问明确需求
2. 明确需求之后，先做计划，等我确定计划可行，再进行下一步
3. 完成计划之后，要给出清晰简洁的报告，说明进行的修改

## MVP 强制规则

在 MVP 未完成前：
- 禁止添加任何非 MVP 功能
- 禁止设计扩展架构
- 禁止优化性能或重构系统
- 只允许实现 docs/mvp.md 中定义的内容

## 防发散规则

如果需求不属于当前功能：
- 必须明确指出“不属于当前任务”
- 不得擅自实现
- 必须回到 MVP 或当前任务范围


## Git 规范

### 分支策略
- main：稳定生产分支（禁止直接提交）
- dev：开发分支（所有开发基于 dev）
- feature/*：功能分支（每个任务一个分支）

---

### 提交规则
- 每次修改必须提交到 feature 分支
- 提交必须语义清晰，例如：
  - feat: 添加移动系统
  - fix: 修复碰撞bug
  - refactor: 重构输入逻辑
提交的粒度应该更细一点

---

### 合并规则
- 所有代码必须通过 PR 合并到 dev
- dev 稳定后再合并到 main
- 禁止直接 push 到 main

---

### agent 强制规则
- 禁止直接修改 main 分支代码
- 必须先创建分支再修改
- 每次修改必须说明影响范围

## 虚拟环境

所有依赖通过conda Cook管理

## 项目常用执行规则

### Unity Editor
- Unity 路径固定为 `D:\unity\Editor\Unity.exe`。
- 需要运行 Unity Editor、batchmode、重建场景、验证编译、构建 Player 时，通常需要提权执行。
- 如果 Unity Editor 已经打开，batchmode 可能无法正常导入脚本或写入场景；先提醒用户关闭 Editor，或等待用户确认已经关闭后再运行。
- 不要先反复用普通权限尝试 Unity 命令；需要运行 Editor 时直接申请提权。

### 常用 Unity 命令
- 重建 MVP 场景：
  `& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildScene -logFile "D:\CookingSimulator\unity-rebuild-<task>.log"`
- 验证 C# 编译：
  `& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -logFile "D:\CookingSimulator\unity-verify-<task>.log"`
- 构建 Windows Player：
  `& "D:\unity\Editor\Unity.exe" -batchmode -quit -projectPath "D:\CookingSimulator" -executeMethod CookingSimulator.Editor.MvpSceneBuilder.BuildWindowsPlayer -logFile "D:\CookingSimulator\unity-build-windows-<task>.log"`

### 验证要求
- 修改场景生成器、Unity UI 绑定、MonoBehaviour 序列化字段后，必须重建 `Assets/Scenes/MVP.unity`。
- 新增 Unity 脚本后，必须通过 Unity 导入生成对应 `.meta` 文件。
- 验证时检查日志中是否有 `error CS`、`Scripts have compiler errors`、`Compiler errors`；Unity 退出码为 0 且无 C# 编译错误才算通过。
- Unity 日志中的授权刷新失败或退出时线程清理信息，如果进程返回码为 0 且没有 C# 编译错误，一般不阻塞本任务。

### Git 执行
- 修改完成并验证后，按任务范围提交到当前 `feature/*` 分支。
- 提交前用 `git status --short --branch` 确认不在 `main`，并确认没有无关文件被暂存。

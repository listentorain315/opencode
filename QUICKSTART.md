# 中国象棋 - 快速启动指南

## 项目已创建完成！

所有游戏文件已生成在 `ChineseChess` 文件夹中。

## 快速启动步骤

### 1. 打开Unity Hub
- 如果没有Unity，请先安装Unity Hub和Unity Editor（推荐2020.3+）

### 2. 创建Unity项目
- 点击 "New Project"
- 选择 "3D" 模板
- 项目名称: `ChineseChessGame`
- 位置: 任意你喜欢的位置
- 点击 "Create project"

### 3. 复制脚本文件
将 `ChineseChess/Assets/Scripts` 文件夹中的所有 `.cs` 文件复制到新项目的 `Assets/Scripts` 目录

### 4. 创建场景
- 在Unity中，点击 File > New Scene
- 保存场景为 `Assets/Scenes/GameScene.unity`

### 5. 设置场景
- 创建一个空GameObject: GameObject > Create Empty
- 命名为 "GameManager"
- 挂载以下脚本:
  - `SceneSetup.cs`
  - `BoardRenderer.cs`  
  - `GameManager.cs`

### 6. 运行游戏
- 点击Unity顶部的 Play 按钮 (▶)
- 游戏将自动开始！

## 游戏说明

- **开局**: 红黑方随机分配（50%概率）
- **操作**: 点击棋子选中，再点击目标位置移动
- **AI**: 使用Alpha-Beta剪枝算法 + 开局库
- **功能**: 支持悔棋、重新开始

## 自定义设置

选中GameManager对象，在Inspector中可以调整:
- `AI Depth`: 搜索深度（默认4，越大越强但越慢）
- `Use Advanced AI`: 是否使用高级AI
- `AI Move Delay`: AI思考延迟

## 文件说明

| 文件 | 功能 |
|------|------|
| `ChessData.cs` | 棋子、位置等数据结构 |
| `ChessRules.cs` | 走棋规则、将军检测 |
| `ChessAI.cs` | 基础AI |
| `AdvancedChessAI.cs` | 高级AI（含开局库） |
| `GameManager.cs` | 游戏主控制器 |
| `BoardRenderer.cs` | 棋盘渲染 |
| `UIManager.cs` | UI管理 |
| `SceneSetup.cs` | 场景自动配置 |

## 需要帮助？

如有问题，请查看README.md文件或反馈问题。

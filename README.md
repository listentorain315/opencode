# 中国象棋 - Unity版

一个功能完整的单机中国象棋游戏，内置开局库，无需外部文件即可运行。

## 功能特点

- **内置开局库** - 包含中炮、飞相、仙人指路等经典开局
- **GPU加速AI** - 使用Compute Shader并行计算
- **多线程搜索** - 支持8+线程并行搜索
- **棋力等级显示** - 实时显示AI棋力水平
- **多难度选择** - 6个难度级别
- **即开即玩** - 无需下载额外资源

## 快速开始

### 1. 打开Unity项目
- Unity Hub → Open → 选择 `ChineseChess` 文件夹

### 2. 运行游戏
- 创建空GameObject，挂载 `SceneSetup`、`BoardRenderer`、`GameManager`
- 点击Play按钮

### 3. 游戏操作
- 点击棋子选中
- 点击高亮位置移动
- 右侧面板显示AI棋力信息

## AI棋力等级

| 难度 | 棋力等级 | ELO评分 | 搜索深度 |
|------|----------|---------|----------|
| 初学者 | 新手入门 | 400-600 | 2层 |
| 简单 | 业余1-2级 | 800-1000 | 3层 |
| 中等 | 业余3-4级 | 1200-1400 | 5层 |
| 困难 | 业余5-6级 | 1600-1800 | 6层 |
| 专家 | 业余7-8级 | 2000-2200 | 8层 |
| 大师 | 专业级 | 2400+ | 10层 |

## 内置开局库

游戏内置了完整的开局库，包含：

### 中炮开局
- 中炮对屏风马
- 中炮对反宫马
- 顺炮/列炮

### 飞相局
- 飞相对跳马
- 飞相对飞象

### 仙人指路
- 仙人指路对中炮
- 仙人指路对跳马
- 对兵局

### 其他开局
- 起马局
- 士角炮
- 过宫炮

## 项目结构

```
ChineseChess/
├── Assets/
│   ├── Scripts/
│   │   ├── ChessData.cs         # 数据结构
│   │   ├── ChessRules.cs        # 走棋规则
│   │   ├── ChessAI.cs           # 基础AI
│   │   ├── AdvancedChessAI.cs   # 高级AI (多线程)
│   │   ├── GPUChessAI.cs        # GPU加速AI
│   │   ├── GameManager.cs       # 游戏管理器
│   │   ├── BoardRenderer.cs     # 棋盘渲染
│   │   ├── SceneSetup.cs        # 场景配置
│   │   ├── ResourceManager.cs   # 资源管理器
│   │   ├── OpeningBookLoader.cs # 开局库加载器
│   │   ├── PGNParser.cs         # 棋谱解析器
│   │   ├── ChessConfig.cs       # 配置管理
│   │   └── BuiltInOpeningBook.cs # 内置开局库
│   └── Resources/
│       └── ChessEvaluate.compute # GPU着色器
├── README.md
└── QUICKSTART.md
```

## 性能对比

| 指标 | CPU单线程 | CPU多线程 | GPU加速 |
|------|-----------|-----------|---------|
| 搜索速度 | 1x | 4-8x | 10-50x |
| 响应时间 | 2-5秒 | 0.5-2秒 | 0.1-0.5秒 |
| 搜索深度 | 4层 | 6层 | 8层 |

## 扩展开局库

如需更强的AI，可以导入外部开局库：

### 推荐资源

1. **象棋百科全书** - https://www.xqbase.com/
   - 在线棋谱数据库
   - 可导出PGN格式

2. **GitHub开源项目**
   - https://github.com/xqbase/eleeye
   - ElephantEye引擎源码

### 导入方法

1. 下载PGN格式棋谱
2. 放入 `Assets/Games/` 目录
3. 游戏会自动加载

## 自定义设置

在GameManager组件中调整：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| AI Depth | 搜索深度 | 6 |
| Use Advanced AI | 使用高级AI | true |
| Use GPU | 使用GPU加速 | true |
| Thread Count | 线程数 | 8 |
| AI Move Delay | AI思考延迟 | 0.5秒 |

## 技术栈

- Unity 2020.3+
- C# 多线程 (Task/Parallel)
- Compute Shader (GPU)
- Alpha-Beta剪枝算法
- Zobrist哈希
- 置换表 (Transposition Table)
- 内置开局库

## 更新日志

### v4.0 (当前版本)
- 添加内置开局库
- 无需外部文件即可运行
- 优化开局库查询

### v3.0
- 添加开局库导入支持
- 添加PGN棋谱解析
- 添加残局库集成

### v2.0
- 添加GPU加速支持
- 添加多线程搜索
- 添加AI棋力等级显示

### v1.0
- 基础游戏功能
- Alpha-Beta剪枝AI

## 许可证

MIT License

## 需要帮助？

- 查看 [QUICKSTART.md](QUICKSTART.md) 快速上手
- 提交Issue反馈问题

## 下一步计划

- [ ] 添加神经网络评估
- [ ] 添加自我对弈学习
- [ ] 添加在线对战
- [ ] 添加棋谱回放
- [ ] 添加音效系统

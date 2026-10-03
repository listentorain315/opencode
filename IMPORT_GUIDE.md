# 棋谱资源导入指南

## 快速导入步骤

### 1. 下载开局库

**推荐资源：**

| 资源 | 下载地址 | 格式 | 大小 |
|------|----------|------|------|
| Pikafish开局库 | https://github.com/Pikafish/Pikafish/releases | .obk | ~10MB |
| 象棋百科全书开局库 | https://www.xqbase.com/ | .abk | ~50MB |
| Stockfish开局库 | https://github.com/official-stockfish/books | .bin | ~20MB |

**下载步骤：**

1. 访问 https://github.com/Pikafish/Pikafish/releases
2. 下载最新版本的 `pikafish.obk` 文件
3. 保存到项目的 `Assets/Books/` 目录

### 2. 下载棋谱数据库

**推荐资源：**

| 资源 | 下载地址 | 格式 | 大小 |
|------|----------|------|------|
| 象棋百科全书棋谱 | https://www.xqbase.com/ | .pgn | ~500MB |
| 大师对局精选 | https://www.qizhong.net/ | .pgn | ~100MB |
| 职业比赛棋谱 | https://tx.qq.com/ | .json | ~50MB |

### 3. 下载残局库（可选）

| 资源 | 下载地址 | 格式 | 大小 |
|------|----------|------|------|
| Syzygy残局库 | https://tablebase.lichess.syzygy-chess.com/ | .rtbw | ~2GB |
| Gaviota残局库 | https://github.com/michiguel/Gaviota-Tablebases | .gtb | ~1GB |

## 目录结构

```
ChineseChess/
├── Assets/
│   ├── Books/
│   │   ├── opening.obk          # 开局库
│   │   ├── opening.abk          # 备用开局库
│   │   └── endgame/             # 残局库
│   │       ├── syzygy/
│   │       └── gaviota/
│   ├── Games/
│   │   ├── master.pgn           # 大师棋谱
│   │   └── professional.pgn    # 职业棋谱
│   └── Resources/
│       └── BuiltIn/             # 内置资源
```

## 导入方法

### 方法一：自动加载

1. 将资源文件放入对应目录
2. 运行游戏时会自动加载
3. 控制台会显示加载进度

### 方法二：手动加载

```csharp
// 在GameManager中调用
ResourceManager.Instance.LoadAllResources();
```

### 方法三：代码导入

```csharp
// 加载开局库
OpeningBookLoader loader = new OpeningBookLoader();
loader.LoadBook("path/to/opening.obk", BookFormat.OBK);

// 加载棋谱
PGNParser parser = new PGNParser();
var games = parser.ParseFile("path/to/games.pgn");

// 保存为内部格式
loader.SaveAsInternalFormat("Assets/Books/internal.txt");
```

## 支持的格式

### 开局库格式

| 格式 | 扩展名 | 说明 |
|------|--------|------|
| OBK | .obk | Pikafish/Stockfish格式 |
| ABK | .abk | 象棋百科全书格式 |
| BIN | .bin | 通用二进制格式 |
| TXT | .txt | 文本格式 |
| PGN | .pgn | 棋谱格式 |

### 文本格式示例

```
# 开局库文本格式
# 格式: 局面 走法 权重
start h2e2 100
start b2e2 95
start h0g2 90
h2e2 h9g7 95
h2e2 b9c7 90
```

## 验证导入

### 检查资源状态

```csharp
// 获取资源信息
var info = ResourceManager.Instance.GetResourceInfo();
foreach (var kvp in info)
{
    Debug.Log($"{kvp.Key}: {kvp.Value}");
}
```

### 测试开局库

```csharp
// 测试查询
string bestMove = ResourceManager.Instance.QueryOpeningBook("start");
Debug.Log($"Best opening move: {bestMove}");
```

## 常见问题

### Q: 开局库加载失败？

A: 检查以下几点：
1. 文件路径是否正确
2. 文件格式是否支持
3. 文件是否损坏

### Q: 棋谱解析错误？

A: 可能原因：
1. 棋谱格式不标准
2. 编码问题（尝试UTF-8）
3. 棋谱包含特殊字符

### Q: 如何提升AI棋力？

A: 建议：
1. 使用更大的开局库
2. 增加搜索深度
3. 使用GPU加速
4. 导入更多棋谱

## 资源推荐优先级

1. **开局库**（必需）
   - 优先下载Pikafish开局库
   - 提升开局阶段棋力

2. **棋谱数据库**（推荐）
   - 下载大师对局棋谱
   - 学习经典走法

3. **残局库**（可选）
   - 下载Syzygy残局库
   - 提升残局阶段棋力

## 技术支持

如有问题，请提供：
1. 资源文件格式
2. 错误信息
3. 文件大小

我会帮你解决问题！

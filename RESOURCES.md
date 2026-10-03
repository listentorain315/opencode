# 棋谱资源获取指南

## 推荐资源网站

### 1. 开局库 (Opening Books)

| 网站 | 资源 | 格式 | 说明 |
|------|------|------|------|
| [象棋百科全书](https://www.xqbase.com/) | 开局库大全 | .obook, .abk | 最全面的中文象棋资源 |
| [棋中论坛](http://www.qizhong.net/) | 开局库合集 | .bin, .dat | 活跃的象棋社区 |
| [Pikafish](https://github.com/Pikafish/Pikafish) | 开源开局库 | .obook | 开源引擎自带开局库 |

### 2. 残局库 (Endgame Databases)

| 网站 | 资源 | 格式 | 说明 |
|------|------|------|------|
| [Syzygy](https://tablebase.lichess.syzygy-chess.com/) | 残局表 | .rtbw, .rtbz | 专业残局库 |
| [Gaviota](https://github.com/michiguel/Gaviota-Tablebases) | 残局库 | .gtb | 开源残局库 |
| [象棋百科全书](https://www.xqbase.com/) | 残局精选 | .xqf | 中文残局库 |

### 3. 棋谱数据 (Game Records)

| 网站 | 资源 | 格式 | 说明 |
|------|------|------|------|
| [象棋百科全书](https://www.xqbase.com/) | 大师棋谱 | .xqf, .pgn | 数十万局大师对局 |
| [棋中论坛](http://www.qizhong.net/) | 棋谱合集 | .pgn, .txt | 用户上传的棋谱 |
| [天天象棋](https://tx.qq.com/) | 职业棋谱 | JSON | 职业比赛棋谱 |
| [象棋巫师](http://www.xqwizard.com/) | 棋谱数据库 | .xqf | 专业象棋软件 |

### 4. 开源引擎参考

| 项目 | 说明 | 链接 |
|------|------|------|
| **Pikafish** | 最强开源象棋引擎 | https://github.com/Pikafish/Pikafish |
| **ElephantEye** | 经典象棋引擎 | https://github.com/ElephantEye/ElephantEye |
| **UCCI引擎** | 标准象棋引擎协议 | https://www.xqbase.com/ucci/ |

## 如何导入资源到项目

### 导入开局库

```csharp
// 在 AdvancedChessAI.cs 中添加开局库加载
private void LoadOpeningBook(string filePath)
{
    if (!File.Exists(filePath)) return;
    
    string[] lines = File.ReadAllLines(filePath);
    foreach (string line in lines)
    {
        // 解析格式: "position move weight"
        string[] parts = line.Split(' ');
        if (parts.Length >= 3)
        {
            string key = parts[0];
            string move = parts[1];
            int weight = int.Parse(parts[2]);
            openingBook[key + "_" + move] = weight;
        }
    }
}
```

### 导入棋谱

```csharp
// 棋谱解析示例
public class PgnParser
{
    public List<Move> ParsePgn(string pgnContent)
    {
        List<Move> moves = new List<Move>();
        // 解析PGN格式棋谱
        // 格式: 1. 炮二平五 马8进7 2. 马二进三 ...
        return moves;
    }
}
```

## 棋力提升建议

### 当前AI vs 专业引擎对比

| 特性 | 当前实现 | Pikafish |
|------|----------|----------|
| 搜索深度 | 4-6层 | 20+层 |
| 开局库 | 基础 | 百万局面 |
| 残局库 | 无 | 完整 |
| 评估函数 | 位置表 | 神经网络 |
| 搜索算法 | Alpha-Beta | NNUE + MCTS |

### 快速提升方案

1. **使用Pikafish开局库**
   - 下载: https://github.com/Pikafish/Pikafish/releases
   - 文件: `pikafish.obk`

2. **集成NNUE评估**
   - 参考Pikafish的NNUE实现
   - 需要训练数据和模型

3. **添加残局库**
   - 下载Syzygy残局库
   - 文件约1-2GB

## 资源下载汇总

### 推荐下载清单

1. **开局库** (必需)
   - 象棋百科全书开局库合集
   - 大小: ~50MB

2. **棋谱数据** (推荐)
   - 大师对局数据库
   - 大小: ~500MB

3. **残局库** (可选)
   - Syzygy 3-5子残局
   - 大小: ~2GB

### 免费资源获取

- 象棋百科全书: https://www.xqbase.com/
  - 注册后可免费下载部分资源
  - VIP会员可下载全部资源

- GitHub开源项目
  - Pikafish: 免费开源
  - ElephantEye: 免费开源

## 注意事项

1. **版权问题**
   - 部分棋谱可能有版权
   - 开源项目通常可免费使用

2. **格式兼容**
   - 不同引擎使用不同格式
   - 可能需要格式转换

3. **性能影响**
   - 大型开局库会增加加载时间
   - 残局库需要较大内存

## 需要帮助？

如需帮助导入特定格式的棋谱资源，请提供：
1. 资源文件格式
2. 来源网站
3. 文件大小

我可以帮你编写对应的解析代码！

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class OpeningBookLoader
{
    // 支持的开局库格式
    public enum BookFormat
    {
        OBK,    // Pikafish/Stockfish格式
        ABK,    // 象棋百科全书格式
        BIN,    // 二进制格式
        TXT,    // 文本格式
        PGN,    // 棋谱格式
        XQF     // 象棋大师格式
    }

    // 开局库条目
    [Serializable]
    public struct BookEntry
    {
        public string position;  // 局面FEN
        public string move;      // 走法
        public int weight;       // 权重
        public int win;          // 胜局数
        public int loss;         // 负局数
        public int draw;         // 平局数
    }

    // 开局库统计
    public class BookStats
    {
        public int totalEntries;
        public int uniquePositions;
        public Dictionary<string, int> positionCounts;
        public Dictionary<string, int> moveCounts;

        public BookStats()
        {
            positionCounts = new Dictionary<string, int>();
            moveCounts = new Dictionary<string, int>();
        }
    }

    private Dictionary<string, List<BookEntry>> bookData;
    private BookStats stats;

    public OpeningBookLoader()
    {
        bookData = new Dictionary<string, List<BookEntry>>();
        stats = new BookStats();
    }

    // 加载开局库
    public bool LoadBook(string filePath, BookFormat format = BookFormat.AUTO)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Opening book file not found: {filePath}");
            return false;
        }

        if (format == BookFormat.AUTO)
            format = DetectFormat(filePath);

        try
        {
            switch (format)
            {
                case BookFormat.OBK:
                    return LoadOBKFormat(filePath);
                case BookFormat.ABK:
                    return LoadABKFormat(filePath);
                case BookFormat.BIN:
                    return LoadBINFormat(filePath);
                case BookFormat.TXT:
                    return LoadTXTFormat(filePath);
                case BookFormat.PGN:
                    return LoadPGNFormat(filePath);
                case BookFormat.XQF:
                    return LoadXQFFormat(filePath);
                default:
                    Debug.LogError($"Unsupported format: {format}");
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load opening book: {e.Message}");
            return false;
        }
    }

    // 自动检测格式
    private BookFormat DetectFormat(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        switch (ext)
        {
            case ".obk": return BookFormat.OBK;
            case ".abk": return BookFormat.ABK;
            case ".bin": return BookFormat.BIN;
            case ".txt":
            case ".csv": return BookFormat.TXT;
            case ".pgn": return BookFormat.PGN;
            case ".xqf": return BookFormat.XQF;
            default: return BookFormat.TXT;
        }
    }

    // 加载OBK格式 (Pikafish/Stockfish)
    private bool LoadOBKFormat(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // 读取文件头
            string magic = new string(reader.ReadChars(4));
            if (magic != "OBK\0")
            {
                Debug.LogError("Invalid OBK file format");
                return false;
            }

            int version = reader.ReadInt32();
            int entryCount = reader.ReadInt32();

            Debug.Log($"Loading OBK book: {entryCount} entries, version {version}");

            for (int i = 0; i < entryCount; i++)
            {
                // 读取条目
                BookEntry entry = new BookEntry();
                
                // 读取局面哈希 (8字节)
                ulong hash = reader.ReadUInt64();
                
                // 读取走法
                byte fromRow = reader.ReadByte();
                byte fromCol = reader.ReadByte();
                byte toRow = reader.ReadByte();
                byte toCol = reader.ReadByte();
                
                entry.move = $"{(char)('a' + fromCol)}{9 - fromRow}{(char)('a' + toCol)}{9 - toRow}";
                
                // 读取权重
                entry.weight = reader.ReadInt32();
                
                // 读取统计
                entry.win = reader.ReadInt32();
                entry.loss = reader.ReadInt32();
                entry.draw = reader.ReadInt32();

                // 转换为内部格式
                string posKey = hash.ToString();
                entry.position = posKey;

                if (!bookData.ContainsKey(posKey))
                    bookData[posKey] = new List<BookEntry>();
                
                bookData[posKey].Add(entry);
                stats.totalEntries++;
            }
        }

        Debug.Log($"Loaded {stats.totalEntries} entries from OBK book");
        return true;
    }

    // 加载ABK格式 (象棋百科全书)
    private bool LoadABKFormat(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // ABK格式头
            byte[] header = reader.ReadBytes(32);
            string headerStr = System.Text.Encoding.ASCII.GetString(header);
            
            if (!headerStr.Contains("ABK"))
            {
                Debug.LogError("Invalid ABK file format");
                return false;
            }

            // 读取条目数
            int entryCount = reader.ReadInt32();
            Debug.Log($"Loading ABK book: {entryCount} entries");

            for (int i = 0; i < entryCount; i++)
            {
                BookEntry entry = new BookEntry();
                
                // 读取FEN (固定长度)
                byte[] fenBytes = reader.ReadBytes(64);
                entry.position = System.Text.Encoding.ASCII.GetString(fenBytes).TrimEnd('\0');
                
                // 读取走法
                byte[] moveBytes = reader.ReadBytes(8);
                entry.move = System.Text.Encoding.ASCII.GetString(moveBytes).TrimEnd('\0');
                
                // 读取权重
                entry.weight = reader.ReadInt32();

                if (!bookData.ContainsKey(entry.position))
                    bookData[entry.position] = new List<BookEntry>();
                
                bookData[entry.position].Add(entry);
                stats.totalEntries++;
            }
        }

        Debug.Log($"Loaded {stats.totalEntries} entries from ABK book");
        return true;
    }

    // 加载BIN格式 (通用二进制)
    private bool LoadBINFormat(string filePath)
    {
        byte[] data = File.ReadAllBytes(filePath);
        
        // 尝试解析为简单的键值对格式
        using (MemoryStream ms = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(ms))
        {
            while (ms.Position < ms.Length)
            {
                try
                {
                    BookEntry entry = new BookEntry();
                    
                    // 读取键长度
                    byte keyLen = reader.ReadByte();
                    byte[] keyBytes = reader.ReadBytes(keyLen);
                    entry.position = System.Text.Encoding.ASCII.GetString(keyBytes);
                    
                    // 读取走法长度
                    byte moveLen = reader.ReadByte();
                    byte[] moveBytes = reader.ReadBytes(moveLen);
                    entry.move = System.Text.Encoding.ASCII.GetString(moveBytes);
                    
                    // 读取权重
                    entry.weight = reader.ReadInt32();

                    if (!bookData.ContainsKey(entry.position))
                        bookData[entry.position] = new List<BookEntry>();
                    
                    bookData[entry.position].Add(entry);
                    stats.totalEntries++;
                }
                catch
                {
                    break;
                }
            }
        }

        Debug.Log($"Loaded {stats.totalEntries} entries from BIN book");
        return true;
    }

    // 加载TXT格式 (文本)
    private bool LoadTXTFormat(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            string[] parts = line.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                BookEntry entry = new BookEntry();
                entry.position = parts[0];
                entry.move = parts[1];
                entry.weight = parts.Length > 2 ? int.Parse(parts[2]) : 100;

                if (!bookData.ContainsKey(entry.position))
                    bookData[entry.position] = new List<BookEntry>();
                
                bookData[entry.position].Add(entry);
                stats.totalEntries++;
            }
        }

        Debug.Log($"Loaded {stats.totalEntries} entries from TXT book");
        return true;
    }

    // 加载PGN格式 (棋谱)
    private bool LoadPGNFormat(string filePath)
    {
        string content = File.ReadAllText(filePath);
        List<List<string>> games = ParsePGN(content);

        foreach (var game in games)
        {
            string currentPosition = "start";
            
            foreach (string moveStr in game)
            {
                BookEntry entry = new BookEntry();
                entry.position = currentPosition;
                entry.move = ConvertMoveNotation(moveStr);
                entry.weight = 100;

                if (!bookData.ContainsKey(currentPosition))
                    bookData[currentPosition] = new List<BookEntry>();
                
                bookData[currentPosition].Add(entry);
                stats.totalEntries++;

                // 更新局面
                currentPosition = GetNextPosition(currentPosition, entry.move);
            }
        }

        Debug.Log($"Loaded {stats.totalEntries} entries from {games.Count} PGN games");
        return true;
    }

    // 加载XQF格式 (象棋大师)
    private bool LoadXQFFormat(string filePath)
    {
        // XQF是象棋大师软件的专有格式
        // 这里提供基本的解析框架
        Debug.LogWarning("XQF format requires specialized parser. Using fallback.");
        return LoadTXTFormat(filePath);
    }

    // 解析PGN文件
    private List<List<string>> ParsePGN(string pgnContent)
    {
        List<List<string>> games = new List<List<string>>();
        List<string> currentGame = null;

        string[] lines = pgnContent.Split('\n');
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            if (trimmed.StartsWith("["))
            {
                // 标签行，忽略
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (currentGame != null && currentGame.Count > 0)
                {
                    games.Add(currentGame);
                    currentGame = null;
                }
                continue;
            }

            if (currentGame == null)
                currentGame = new List<string>();

            // 解析走法
            string[] moves = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string move in moves)
            {
                // 过滤掉回合号 (1. 2. 等)
                if (System.Text.RegularExpressions.Regex.IsMatch(move, @"^\d+\."))
                    continue;
                
                // 过滤掉结果
                if (move == "1-0" || move == "0-1" || move == "1/2-1/2" || move == "*")
                    continue;

                currentGame.Add(move);
            }
        }

        if (currentGame != null && currentGame.Count > 0)
            games.Add(currentGame);

        return games;
    }

    // 转换走法表示法
    private string ConvertMoveNotation(string move)
    {
        // 处理中文表示法: 炮二平五, 马8进7 等
        // 转换为坐标表示法: h2e2, b0c2 等
        
        if (move.Length >= 4)
        {
            // 已经是坐标格式
            if (char.IsLetter(move[0]) && char.IsDigit(move[1]))
                return move.ToLower();
        }

        // 中文走法转换
        return ConvertChineseMove(move);
    }

    private string ConvertChineseMove(string move)
    {
        // 中文数字映射
        Dictionary<char, int> chineseNums = new Dictionary<char, int>
        {
            {'一', 1}, {'二', 2}, {'三', 3}, {'四', 4}, {'五', 5},
            {'六', 6}, {'七', 7}, {'八', 8}, {'九', 9},
            {'１', 1}, {'２', 2}, {'３', 3}, {'４', 4}, {'５', 5},
            {'６', 6}, {'７', 7}, {'８', 8}, {'９', 9},
            {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4}, {'5', 5},
            {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9}
        };

        // 棋子映射
        Dictionary<char, char> pieceMap = new Dictionary<char, char>
        {
            {'帅', 'K'}, {'将', 'k'},
            {'仕', 'A'}, {'士', 'a'},
            {'相', 'E'}, {'象', 'e'},
            {'马', 'H'}, {'馬', 'h'},
            {'车', 'R'}, {'車', 'r'},
            {'炮', 'C'}, {'砲', 'c'},
            {'兵', 'P'}, {'卒', 'p'}
        };

        try
        {
            if (move.Length < 4) return move;

            char piece = move[0];
            char col1 = move[1];
            char action = move[2];
            char col2 = move[3];

            // 获取起始列
            int fromCol;
            if (chineseNums.ContainsKey(col1))
                fromCol = chineseNums[col1];
            else
                return move;

            // 获取目标列
            int toCol;
            if (chineseNums.ContainsKey(col2))
                toCol = chineseNums[col2];
            else
                return move;

            // 根据动作确定行变化
            int fromRow = 0;
            int toRow = 0;

            if (action == '平')
            {
                // 平移
                toRow = fromRow;
            }
            else if (action == '进')
            {
                // 前进
                if (piece == '兵' || piece == '卒' || piece == '马' || piece == '车' || piece == '炮')
                {
                    toRow = fromRow + 1;
                }
                else
                {
                    toRow = fromRow - 1;
                }
            }
            else if (action == '退')
            {
                // 后退
                if (piece == '兵' || piece == '卒' || piece == '马' || piece == '车' || piece == '炮')
                {
                    toRow = fromRow - 1;
                }
                else
                {
                    toRow = fromRow + 1;
                }
            }

            // 转换为坐标格式
            char fromColChar = (char)('a' + fromCol - 1);
            char toColChar = (char)('a' + toCol - 1);

            return $"{fromColChar}{fromRow}{toColChar}{toRow}";
        }
        catch
        {
            return move;
        }
    }

    private string GetNextPosition(string currentPosition, string move)
    {
        // 简化的局面更新，实际应该应用走法
        return currentPosition + "_" + move;
    }

    // 查询开局库
    public List<BookEntry> QueryPosition(string position)
    {
        if (bookData.TryGetValue(position, out List<BookEntry> entries))
        {
            // 按权重排序
            return entries.OrderByDescending(e => e.weight).ToList();
        }
        return new List<BookEntry>();
    }

    // 获取最佳走法
    public string GetBestMove(string position)
    {
        List<BookEntry> entries = QueryPosition(position);
        
        if (entries.Count == 0)
            return null;

        // 加权随机选择
        int totalWeight = entries.Sum(e => e.weight);
        int random = UnityEngine.Random.Range(0, totalWeight);
        
        int currentWeight = 0;
        foreach (var entry in entries)
        {
            currentWeight += entry.weight;
            if (random < currentWeight)
                return entry.move;
        }

        return entries[0].move;
    }

    // 获取统计信息
    public BookStats GetStats()
    {
        stats.uniquePositions = bookData.Count;
        
        foreach (var kvp in bookData)
        {
            stats.positionCounts[kvp.Key] = kvp.Value.Count;
            
            foreach (var entry in kvp.Value)
            {
                if (!stats.moveCounts.ContainsKey(entry.move))
                    stats.moveCounts[entry.move] = 0;
                stats.moveCounts[entry.move]++;
            }
        }

        return stats;
    }

    // 保存为内部格式
    public bool SaveAsInternalFormat(string outputPath)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                foreach (var kvp in bookData)
                {
                    foreach (var entry in kvp.Value)
                    {
                        writer.WriteLine($"{entry.position} {entry.move} {entry.weight}");
                    }
                }
            }
            Debug.Log($"Saved {stats.totalEntries} entries to {outputPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save book: {e.Message}");
            return false;
        }
    }

    // 合并多个开局库
    public void MergeBook(OpeningBookLoader other)
    {
        foreach (var kvp in other.bookData)
        {
            if (!bookData.ContainsKey(kvp.Key))
                bookData[kvp.Key] = new List<BookEntry>();
            
            bookData[kvp.Key].AddRange(kvp.Value);
            stats.totalEntries += kvp.Value.Count;
        }
    }

    // 清理低权重条目
    public void PruneBook(int minWeight = 10)
    {
        foreach (var kvp in bookData)
        {
            bookData[kvp.Key] = kvp.Value.Where(e => e.weight >= minWeight).ToList();
        }
        
        // 移除空条目
        var emptyKeys = bookData.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
        foreach (string key in emptyKeys)
        {
            bookData.Remove(key);
        }

        stats.totalEntries = bookData.Values.Sum(entries => entries.Count);
    }
}

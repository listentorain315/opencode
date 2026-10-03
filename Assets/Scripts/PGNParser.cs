using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class PGNParser
{
    // 棋谱游戏数据
    [Serializable]
    public class GameRecord
    {
        public string event_name;
        public string site;
        public string date;
        public string round;
        public string white;
        public string black;
        public string result;
        public string fen;
        public List<MoveRecord> moves;
        public Dictionary<string, string> tags;

        public GameRecord()
        {
            moves = new List<MoveRecord>();
            tags = new Dictionary<string, string>();
        }
    }

    // 走法记录
    [Serializable]
    public class MoveRecord
    {
        public int moveNumber;
        public string notation;      // 原始记谱法
        public string coordinate;    // 坐标记谱法
        public string piece;         // 棋子类型
        public int fromRow;
        public int fromCol;
        public int toRow;
        public int toCol;
        public bool isCapture;
        public bool isCheck;
        public bool isCheckmate;

        public MoveRecord()
        {
        }

        public MoveRecord(int number, string not)
        {
            moveNumber = number;
            notation = not;
        }
    }

    // 中文数字映射
    private static readonly Dictionary<char, int> CHINESE_NUMS = new Dictionary<char, int>
    {
        {'一', 1}, {'二', 2}, {'三', 3}, {'四', 4}, {'五', 5},
        {'六', 6}, {'七', 7}, {'八', 8}, {'九', 9},
        {'１', 1}, {'２', 2}, {'３', 3}, {'４', 4}, {'５', 5},
        {'６', 6}, {'７', 7}, {'８', 8}, {'９', 9}
    };

    // 棋子名称映射
    private static readonly Dictionary<char, string> PIECE_NAMES = new Dictionary<char, string>
    {
        {'帅', "King"}, {'将', "King"},
        {'仕', "Advisor"}, {'士', "Advisor"},
        {'相', "Elephant"}, {'象', "Elephant"},
        {'马', "Horse"}, {'馬', "Horse"},
        {'车', "Rook"}, {'車', "Rook"},
        {'炮', "Cannon"}, {'砲', "Cannon"},
        {'兵', "Pawn"}, {'卒', "Pawn"}
    };

    // 解析PGN文件
    public List<GameRecord> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"PGN file not found: {filePath}");
            return new List<GameRecord>();
        }

        string content = File.ReadAllText(filePath);
        return ParseContent(content);
    }

    // 解析PGN内容
    public List<GameRecord> ParseContent(string pgnContent)
    {
        List<GameRecord> games = new List<GameRecord>();
        
        // 分割多个游戏
        string[] gameBlocks = Regex.Split(pgnContent, @"\n\s*\n");
        
        GameRecord currentGame = null;
        
        foreach (string block in gameBlocks)
        {
            if (string.IsNullOrWhiteSpace(block))
                continue;

            string trimmed = block.Trim();
            
            // 检查是否是标签
            if (trimmed.StartsWith("["))
            {
                if (currentGame == null)
                    currentGame = new GameRecord();
                
                ParseTags(trimmed, currentGame);
            }
            // 检查是否是走法
            else if (currentGame != null)
            {
                ParseMoves(trimmed, currentGame);
                games.Add(currentGame);
                currentGame = null;
            }
        }

        Debug.Log($"Parsed {games.Count} games from PGN");
        return games;
    }

    // 解析标签
    private void ParseTags(string tagBlock, GameRecord game)
    {
        MatchCollection matches = Regex.Matches(tagBlock, @"\[(\w+)\s+""([^""]*)""\]");
        
        foreach (Match match in matches)
        {
            string key = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            
            game.tags[key] = value;
            
            switch (key)
            {
                case "Event": game.event_name = value; break;
                case "Site": game.site = value; break;
                case "Date": game.date = value; break;
                case "Round": game.round = value; break;
                case "White": game.white = value; break;
                case "Black": game.black = value; break;
                case "Result": game.result = value; break;
                case "FEN": game.fen = value; break;
            }
        }
    }

    // 解析走法
    private void ParseMoves(string moveBlock, GameRecord game)
    {
        // 清理走法文本
        string cleaned = moveBlock
            .Replace("\n", " ")
            .Replace("\r", "")
            .Trim();

        // 移除结果标记
        cleaned = Regex.Replace(cleaned, @"\s*(1-0|0-1|1/2-1/2|\*)\s*$", "");

        // 分割走法
        string[] parts = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        int moveNumber = 1;
        bool isWhiteMove = true;

        foreach (string part in parts)
        {
            // 跳过回合号
            if (Regex.IsMatch(part, @"^\d+\."))
                continue;

            if (string.IsNullOrWhiteSpace(part))
                continue;

            MoveRecord move = new MoveRecord(moveNumber, part);
            move.coordinate = ConvertNotation(part);
            
            // 解析走法细节
            ParseMoveDetails(move, part);
            
            game.moves.Add(move);

            if (!isWhiteMove)
                moveNumber++;
            
            isWhiteMove = !isWhiteMove;
        }
    }

    // 解析走法细节
    private void ParseMoveDetails(MoveRecord move, string notation)
    {
        // 检查吃子
        move.isCapture = notation.Contains("x") || notation.Contains("X") || notation.Contains("吃");
        
        // 检查将军
        move.isCheck = notation.Contains("+") || notation.Contains("将");
        
        // 检查将杀
        move.isCheckmate = notation.Contains("#") || notation.Contains("杀");

        // 解析中文记谱法
        if (ContainsChinese(notation))
        {
            ParseChineseNotation(move, notation);
        }
        // 解析坐标记谱法
        else if (Regex.IsMatch(notation, @"^[a-i]\d[a-i]\d$"))
        {
            ParseCoordinateNotation(move, notation);
        }
    }

    // 解析中文记谱法
    private void ParseChineseNotation(MoveRecord move, string notation)
    {
        try
        {
            if (notation.Length < 4)
                return;

            // 获取棋子
            char pieceChar = notation[0];
            if (PIECE_NAMES.ContainsKey(pieceChar))
                move.piece = PIECE_NAMES[pieceChar];

            // 获取起始列
            char col1Char = notation[1];
            if (CHINESE_NUMS.ContainsKey(col1Char))
                move.fromCol = CHINESE_NUMS[col1Char] - 1;

            // 获取动作
            char action = notation[2];
            
            // 获取目标
            char col2Char = notation[3];
            if (CHINESE_NUMS.ContainsKey(col2Char))
                move.toCol = CHINESE_NUMS[col2Char] - 1;

            // 计算行
            if (action == '平')
            {
                move.toRow = move.fromRow;
            }
            else if (action == '进')
            {
                // 前进逻辑
                if (pieceChar == '兵' || pieceChar == '卒' || 
                    pieceChar == '马' || pieceChar == '车' || pieceChar == '炮')
                {
                    move.toRow = move.fromRow + 1;
                }
                else
                {
                    move.toRow = move.fromRow - 1;
                }
                
                // 如果有额外数字，表示前进步数
                if (notation.Length > 4 && CHINESE_NUMS.ContainsKey(notation[4]))
                {
                    int steps = CHINESE_NUMS[notation[4]];
                    move.toRow = move.fromRow + steps;
                }
            }
            else if (action == '退')
            {
                // 后退逻辑
                if (pieceChar == '兵' || pieceChar == '卒' || 
                    pieceChar == '马' || pieceChar == '车' || pieceChar == '炮')
                {
                    move.toRow = move.fromRow - 1;
                }
                else
                {
                    move.toRow = move.fromRow + 1;
                }
                
                if (notation.Length > 4 && CHINESE_NUMS.ContainsKey(notation[4]))
                {
                    int steps = CHINESE_NUMS[notation[4]];
                    move.toRow = move.fromRow - steps;
                }
            }

            // 更新坐标记谱法
            move.coordinate = $"{(char)('a' + move.fromCol)}{move.fromRow}{(char)('a' + move.toCol)}{move.toRow}";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse Chinese notation: {notation}, error: {e.Message}");
        }
    }

    // 解析坐标记谱法
    private void ParseCoordinateNotation(MoveRecord move, string notation)
    {
        try
        {
            move.fromCol = notation[0] - 'a';
            move.fromRow = notation[1] - '0';
            move.toCol = notation[2] - 'a';
            move.toRow = notation[3] - '0';
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse coordinate notation: {notation}, error: {e.Message}");
        }
    }

    // 检查是否包含中文
    private bool ContainsChinese(string text)
    {
        return Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
    }

    // 转换为标准记谱法
    public string ConvertToStandard(string notation)
    {
        if (Regex.IsMatch(notation, @"^[a-i]\d[a-i]\d$"))
            return notation;

        // 尝试转换中文记谱法
        MoveRecord move = new MoveRecord(0, notation);
        ParseChineseNotation(move, notation);
        return move.coordinate;
    }

    // 批量转换棋谱
    public List<List<string>> ConvertGamesToMoves(List<GameRecord> games)
    {
        List<List<string>> allMoves = new List<List<string>>();
        
        foreach (var game in games)
        {
            List<string> moves = game.moves
                .Where(m => !string.IsNullOrEmpty(m.coordinate))
                .Select(m => m.coordinate)
                .ToList();
            
            if (moves.Count > 0)
                allMoves.Add(moves);
        }

        return allMoves;
    }

    // 导出为标准PGN格式
    public string ExportToPGN(GameRecord game)
    {
        var sb = new System.Text.StringBuilder();
        
        // 写入标签
        foreach (var tag in game.tags)
        {
            sb.AppendLine($"[{tag.Key} \"{tag.Value}\"]");
        }
        sb.AppendLine();

        // 写入走法
        for (int i = 0; i < game.moves.Count; i++)
        {
            var move = game.moves[i];
            
            if (i % 2 == 0)
                sb.Append($"{i / 2 + 1}. ");
            
            sb.Append(move.notation + " ");
        }

        // 写入结果
        sb.AppendLine(game.result ?? "*");
        
        return sb.ToString();
    }

    // 统计棋谱信息
    public Dictionary<string, int> AnalyzeOpenings(List<GameRecord> games, int maxMoves = 10)
    {
        Dictionary<string, int> openings = new Dictionary<string, int>();
        
        foreach (var game in games)
        {
            string opening = "";
            int count = Math.Min(maxMoves, game.moves.Count);
            
            for (int i = 0; i < count; i++)
            {
                opening += game.moves[i].notation + " ";
            }
            
            opening = opening.Trim();
            
            if (!openings.ContainsKey(opening))
                openings[opening] = 0;
            
            openings[opening]++;
        }

        return openings.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, o => o.Value);
    }
}

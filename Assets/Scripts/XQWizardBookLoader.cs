using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class XQWizardBookLoader
{
    // XQWizard BOOK.DAT格式结构
    private struct BookEntry
    {
        public ulong key;       // 局面哈希
        public ushort move;     // 走法
        public ushort weight;   // 权重
        public uint count;      // 访问次数
    }

    private Dictionary<string, List<MoveWeight>> bookData;
    
    public struct MoveWeight
    {
        public string move;
        public int weight;
    }

    public XQWizardBookLoader()
    {
        bookData = new Dictionary<string, List<MoveWeight>>();
    }

    // 加载BOOK.DAT文件
    public bool LoadBook(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Book file not found: {filePath}");
            return false;
        }

        try
        {
            byte[] data = File.ReadAllBytes(filePath);
            Debug.Log($"Loading BOOK.DAT: {data.Length} bytes");

            // XQWizard的BOOK.DAT格式
            // 每个条目16字节
            int entrySize = 16;
            int entryCount = data.Length / entrySize;

            Debug.Log($"Book contains {entryCount} entries");

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                for (int i = 0; i < entryCount; i++)
                {
                    // 读取条目
                    ulong key = reader.ReadUInt64();
                    uint moveData = reader.ReadUInt32();
                    ushort weight = reader.ReadUInt16();
                    ushort flags = reader.ReadUInt16();

                    // 解析走法
                    string moveStr = DecodeMove(moveData);
                    
                    if (!string.IsNullOrEmpty(moveStr))
                    {
                        string posKey = key.ToString();
                        
                        if (!bookData.ContainsKey(posKey))
                            bookData[posKey] = new List<MoveWeight>();
                        
                        bookData[posKey].Add(new MoveWeight
                        {
                            move = moveStr,
                            weight = weight
                        });
                    }
                }
            }

            Debug.Log($"Loaded {bookData.Count} positions from BOOK.DAT");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load BOOK.DAT: {e.Message}");
            return false;
        }
    }

    // 解码走法
    private string DecodeMove(uint moveData)
    {
        // 走法编码方式
        // 从位置 = moveData & 0xFF
        // 到位置 = (moveData >> 8) & 0xFF
        
        int from = (int)(moveData & 0xFF);
        int to = (int)((moveData >> 8) & 0xFF);

        // 转换为坐标
        int fromRow = from / 9;
        int fromCol = from % 9;
        int toRow = to / 9;
        int toCol = to % 9;

        // 验证范围
        if (fromRow < 0 || fromRow > 9 || fromCol < 0 || fromCol > 8)
            return null;
        if (toRow < 0 || toRow > 9 || toCol < 0 || toCol > 8)
            return null;

        // 转换为字符串
        char fromColChar = (char)('a' + fromCol);
        char toColChar = (char)('a' + toCol);
        
        return $"{fromColChar}{fromRow}{toColChar}{toRow}";
    }

    // 查询开局库
    public string QueryPosition(ChessPiece[,] board)
    {
        ulong key = ComputeBoardKey(board);
        string posKey = key.ToString();

        if (bookData.TryGetValue(posKey, out List<MoveWeight> entries))
        {
            if (entries.Count > 0)
            {
                // 按权重排序
                entries.Sort((a, b) => b.weight.CompareTo(a.weight));
                
                // 加权随机选择
                int totalWeight = 0;
                foreach (var entry in entries)
                    totalWeight += entry.weight;

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
        }

        return null;
    }

    // 计算局面哈希
    private ulong ComputeBoardKey(ChessPiece[,] board)
    {
        ulong key = 0;
        
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (!piece.IsEmpty())
                {
                    int pieceIndex = GetPieceIndex(piece);
                    int pos = r * 9 + c;
                    key ^= ZobristTable[pos, pieceIndex];
                }
            }
        }

        return key;
    }

    private int GetPieceIndex(ChessPiece piece)
    {
        int colorOffset = piece.color == PieceColor.Red ? 0 : 7;
        
        switch (piece.type)
        {
            case PieceType.King: return colorOffset + 0;
            case PieceType.Advisor: return colorOffset + 1;
            case PieceType.Elephant: return colorOffset + 2;
            case PieceType.Horse: return colorOffset + 3;
            case PieceType.Rook: return colorOffset + 4;
            case PieceType.Cannon: return colorOffset + 5;
            case PieceType.Pawn: return colorOffset + 6;
            default: return 0;
        }
    }

    // Zobrist哈希表
    private static readonly ulong[,] ZobristTable = InitZobristTable();

    private static ulong[,] InitZobristTable()
    {
        ulong[,] table = new ulong[90, 14];
        System.Random rng = new System.Random(12345);

        for (int i = 0; i < 90; i++)
        {
            for (int j = 0; j < 14; j++)
            {
                byte[] bytes = new byte[8];
                rng.NextBytes(bytes);
                table[i, j] = BitConverter.ToUInt64(bytes, 0);
            }
        }

        return table;
    }

    // 获取统计信息
    public int GetPositionCount()
    {
        return bookData.Count;
    }

    public int GetMoveCount()
    {
        int count = 0;
        foreach (var entries in bookData.Values)
            count += entries.Count;
        return count;
    }
}

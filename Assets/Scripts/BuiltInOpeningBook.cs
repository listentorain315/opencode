using System.Collections.Generic;
using UnityEngine;

public static class BuiltInOpeningBook
{
    // 完整的开局库数据 - 基于经典棋谱
    public static readonly Dictionary<string, List<OpeningEntry>> OPENINGS = new Dictionary<string, List<OpeningEntry>>
    {
        // ==================== 初始局面 ====================
        {"start", new List<OpeningEntry>{
            new OpeningEntry("h2e2", 100, "中炮"),
            new OpeningEntry("b2e2", 95, "中炮"),
            new OpeningEntry("h0g2", 90, "跳马"),
            new OpeningEntry("b0c2", 90, "跳马"),
            new OpeningEntry("c0e2", 85, "飞相"),
            new OpeningEntry("g0e2", 85, "飞相"),
            new OpeningEntry("c3c4", 80, "仙人指路"),
            new OpeningEntry("g3g4", 75, "仙人指路"),
            new OpeningEntry("b2c2", 70, "士角炮"),
            new OpeningEntry("h2f2", 65, "过宫炮"),
        }},
        
        // ==================== 中炮开局 ====================
        {"h2e2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 100, "屏风马"),
            new OpeningEntry("b9c7", 95, "屏风马"),
            new OpeningEntry("c9e7", 90, "飞象"),
            new OpeningEntry("h7e7", 85, "顺炮"),
            new OpeningEntry("b7e7", 80, "列炮"),
            new OpeningEntry("b9a7", 75, "边马"),
            new OpeningEntry("i9h9", 70, "边车"),
        }},
        
        // 中炮对屏风马
        {"h2e2_h9g7", new List<OpeningEntry>{
            new OpeningEntry("b0c2", 100, "跳正马"),
            new OpeningEntry("h0g2", 95, "跳正马"),
            new OpeningEntry("c3c4", 85, "挺兵"),
            new OpeningEntry("b2d2", 80, "车巡河"),
        }},
        
        {"h2e2_b9c7", new List<OpeningEntry>{
            new OpeningEntry("b0c2", 100, "跳正马"),
            new OpeningEntry("h0g2", 95, "跳正马"),
            new OpeningEntry("c3c4", 85, "挺兵"),
        }},
        
        // 中炮对飞象
        {"h2e2_c9e7", new List<OpeningEntry>{
            new OpeningEntry("b0c2", 95, "跳马"),
            new OpeningEntry("h0g2", 90, "跳马"),
            new OpeningEntry("c3c4", 85, "挺兵"),
        }},
        
        // 顺炮
        {"h2e2_h7e7", new List<OpeningEntry>{
            new OpeningEntry("b0c2", 100, "跳马"),
            new OpeningEntry("h0g2", 95, "跳马"),
            new OpeningEntry("h0i2", 85, "跳边马"),
        }},
        
        // 列炮
        {"h2e2_b7e7", new List<OpeningEntry>{
            new OpeningEntry("b0c2", 95, "跳马"),
            new OpeningEntry("h0g2", 90, "跳马"),
        }},
        
        // ==================== 飞相局 ====================
        {"c0e2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 100, "跳马"),
            new OpeningEntry("b9c7", 95, "跳马"),
            new OpeningEntry("c9e7", 90, "飞象"),
            new OpeningEntry("b9a7", 80, "边马"),
        }},
        
        {"c0e2_h9g7", new List<OpeningEntry>{
            new OpeningEntry("h0g2", 100, "跳马"),
            new OpeningEntry("b0c2", 95, "跳马"),
        }},
        
        {"c0e2_c9e7", new List<OpeningEntry>{
            new OpeningEntry("h0g2", 95, "跳马"),
            new OpeningEntry("b0c2", 90, "跳马"),
        }},
        
        // ==================== 仙人指路 ====================
        {"c3c4", new List<OpeningEntry>{
            new OpeningEntry("h7e7", 100, "中炮"),
            new OpeningEntry("h9g7", 95, "跳马"),
            new OpeningEntry("c6c5", 90, "对兵"),
            new OpeningEntry("b9c7", 85, "跳马"),
            new OpeningEntry("c9e7", 80, "飞象"),
        }},
        
        {"c3c4_h7e7", new List<OpeningEntry>{
            new OpeningEntry("h2e2", 100, "还中炮"),
            new OpeningEntry("b0c2", 90, "跳马"),
        }},
        
        {"c3c4_c6c5", new List<OpeningEntry>{
            new OpeningEntry("h2e2", 95, "中炮"),
            new OpeningEntry("h0g2", 90, "跳马"),
        }},
        
        // ==================== 起马局 ====================
        {"h0g2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 100, "跳马"),
            new OpeningEntry("b9c7", 95, "跳马"),
            new OpeningEntry("c9e7", 85, "飞象"),
        }},
        
        {"b0c2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 100, "跳马"),
            new OpeningEntry("b9c7", 95, "跳马"),
        }},
        
        // ==================== 士角炮 ====================
        {"b2c2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 95, "跳马"),
            new OpeningEntry("b9c7", 90, "跳马"),
            new OpeningEntry("c9e7", 85, "飞象"),
        }},
        
        // ==================== 过宫炮 ====================
        {"h2f2", new List<OpeningEntry>{
            new OpeningEntry("h9g7", 90, "跳马"),
            new OpeningEntry("b9c7", 85, "跳马"),
        }},
        
        // ==================== 中局变化 ====================
        // 屏风马双正马
        {"h2e2_h9g7_b0c2_b9c7", new List<OpeningEntry>{
            new OpeningEntry("h0g2", 100, "跳马"),
            new OpeningEntry("c3c4", 90, "挺兵"),
        }},
        
        // 中炮巡河车
        {"h2e2_h9g7_b0c2_b9c7_h0g2", new List<OpeningEntry>{
            new OpeningEntry("h0i2", 95, "出车"),
            new OpeningEntry("c3c4", 90, "挺兵"),
            new OpeningEntry("i0h0", 85, "出车"),
        }},
        
        // 五七炮
        {"h2e2_h9g7_b0c2_b9c7_h0g2_h0i2", new List<OpeningEntry>{
            new OpeningEntry("c0a2", 95, "五七炮"),
            new OpeningEntry("c3c4", 90, "挺兵"),
        }},
        
        // 五六炮
        {"h2e2_h9g7_b0c2_b9c7_h0g2_h0i2_c0a2", new List<OpeningEntry>{
            new OpeningEntry("i0h0", 95, "出车"),
            new OpeningEntry("c3c4", 90, "挺兵"),
        }},
    };

    public struct OpeningEntry
    {
        public string move;
        public int weight;
        public string name;

        public OpeningEntry(string move, int weight, string name)
        {
            this.move = move;
            this.weight = weight;
            this.name = name;
        }
    }

    // 获取开局走法
    public static string GetOpeningMove(string position)
    {
        if (!OPENINGS.TryGetValue(position, out List<OpeningEntry> entries))
            return null;

        // 加权随机选择
        int totalWeight = 0;
        foreach (var entry in entries)
            totalWeight += entry.weight;

        int random = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var entry in entries)
        {
            currentWeight += entry.weight;
            if (random < currentWeight)
                return entry.move;
        }

        return entries[0].move;
    }

    // 获取所有可能的走法
    public static List<OpeningEntry> GetAllMoves(string position)
    {
        if (OPENINGS.TryGetValue(position, out List<OpeningEntry> entries))
            return entries;
        return new List<OpeningEntry>();
    }

    // 检查是否在开局库中
    public static bool HasPosition(string position)
    {
        return OPENINGS.ContainsKey(position);
    }

    // 获取开局名称
    public static string GetOpeningName(string position)
    {
        if (OPENINGS.TryGetValue(position, out List<OpeningEntry> entries))
        {
            if (entries.Count > 0)
                return entries[0].name;
        }
        return null;
    }
}

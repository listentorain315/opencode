using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ChessConfig", menuName = "Chess/Configuration")]
public class ChessConfig : ScriptableObject
{
    [Header("AI Settings")]
    [Range(2, 10)]
    public int searchDepth = 6;
    
    [Range(1, 16)]
    public int threadCount = 8;
    
    public bool useGPU = true;
    public bool useAdvancedAI = true;
    
    [Header("Resource Paths")]
    public string openingBookPath = "Books/opening.obk";
    public string endgameTablePath = "Books/endgame/";
    public string pgnDatabasePath = "Games/master.pgn";
    
    [Header("Performance")]
    public bool useTranspositionTable = true;
    public bool useOpeningBook = true;
    public bool useEndgameTable = false;
    public int maxCacheSize = 10000;
    
    [Header("UI Settings")]
    public bool showAILevel = true;
    public bool showMoveHints = true;
    public float aiMoveDelay = 0.5f;
    
    [Header("Difficulty Presets")]
    public DifficultyPreset currentPreset = DifficultyPreset.Medium;
    
    public enum DifficultyPreset
    {
        Beginner,      // 初学者
        Easy,          // 简单
        Medium,        // 中等
        Hard,          // 困难
        Expert,        // 专家
        Master         // 大师
    }
    
    // 获取难度设置
    public void ApplyPreset(DifficultyPreset preset)
    {
        currentPreset = preset;
        
        switch (preset)
        {
            case DifficultyPreset.Beginner:
                searchDepth = 2;
                threadCount = 1;
                useGPU = false;
                useAdvancedAI = false;
                useOpeningBook = false;
                aiMoveDelay = 1.0f;
                break;
                
            case DifficultyPreset.Easy:
                searchDepth = 3;
                threadCount = 2;
                useGPU = false;
                useAdvancedAI = true;
                useOpeningBook = true;
                aiMoveDelay = 0.8f;
                break;
                
            case DifficultyPreset.Medium:
                searchDepth = 5;
                threadCount = 4;
                useGPU = false;
                useAdvancedAI = true;
                useOpeningBook = true;
                aiMoveDelay = 0.5f;
                break;
                
            case DifficultyPreset.Hard:
                searchDepth = 6;
                threadCount = 8;
                useGPU = true;
                useAdvancedAI = true;
                useOpeningBook = true;
                aiMoveDelay = 0.3f;
                break;
                
            case DifficultyPreset.Expert:
                searchDepth = 8;
                threadCount = 12;
                useGPU = true;
                useAdvancedAI = true;
                useOpeningBook = true;
                aiMoveDelay = 0.2f;
                break;
                
            case DifficultyPreset.Master:
                searchDepth = 10;
                threadCount = 16;
                useGPU = true;
                useAdvancedAI = true;
                useOpeningBook = true;
                useEndgameTable = true;
                aiMoveDelay = 0.1f;
                break;
        }
    }
    
    // 获取AI类型描述
    public string GetAITypeDescription()
    {
        if (useGPU)
            return "GPU加速AI";
        else if (useAdvancedAI)
            return "高级CPU AI";
        else
            return "基础AI";
    }
    
    // 获取棋力等级描述
    public string GetSkillLevelDescription()
    {
        switch (currentPreset)
        {
            case DifficultyPreset.Beginner:
                return "新手入门";
            case DifficultyPreset.Easy:
                return "业余1-2级";
            case DifficultyPreset.Medium:
                return "业余3-4级";
            case DifficultyPreset.Hard:
                return "业余5-6级";
            case DifficultyPreset.Expert:
                return "业余7-8级";
            case DifficultyPreset.Master:
                return "专业级";
            default:
                return "未知";
        }
    }
    
    // 获取ELO评分范围
    public string GetELORange()
    {
        switch (currentPreset)
        {
            case DifficultyPreset.Beginner:
                return "ELO 400-600";
            case DifficultyPreset.Easy:
                return "ELO 800-1000";
            case DifficultyPreset.Medium:
                return "ELO 1200-1400";
            case DifficultyPreset.Hard:
                return "ELO 1600-1800";
            case DifficultyPreset.Expert:
                return "ELO 2000-2200";
            case DifficultyPreset.Master:
                return "ELO 2400+";
            default:
                return "ELO ?";
        }
    }
    
    // 获取完整描述
    public string GetFullDescription()
    {
        return $"{GetAITypeDescription()}\n棋力: {GetSkillLevelDescription()}\n{GetELORange()}";
    }
}

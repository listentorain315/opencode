using System;
using System.Collections.Generic;
using System.Text;
using GameMusicPlayer.Models;

namespace GameMusicPlayer.Services
{
    /// <summary>
    /// 简谱到键盘脚本转换器
    /// 按照规则将数字简谱或键盘字母转换为按键事件
    /// </summary>
    public static class NotationParser
    {
        // 数字音符到键盘映射
        private static readonly Dictionary<char, string> NumberKeyMap = new()
        {
            { '1', "z" }, { '2', "x" }, { '3', "c" }, { '4', "v" },
            { '5', "b" }, { '6', "n" }, { '7', "m" },
        };

        // 键盘字母到键盘映射
        private static readonly Dictionary<char, string> LetterKeyMap = new()
        {
            { 'z', "z" }, { 'x', "x" }, { 'c', "c" }, { 'v', "v" },
            { 'b', "b" }, { 'n', "n" }, { 'm', "m" },
        };

        /// <summary>
        /// 解析简谱为音符列表
        /// </summary>
        /// <param name="input">简谱输入</param>
        /// <param name="bpm">BPM（默认120）</param>
        /// <returns>音符列表</returns>
        public static List<Note> Parse(string input, int bpm = 120)
        {
            var notes = new List<Note>();
            if (string.IsNullOrWhiteSpace(input)) return notes;

            if (bpm <= 0) bpm = 120;
            double beatMs = 60000.0 / bpm;  // 1拍的毫秒数

            var lines = input.Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                ParseLine(line, notes, beatMs);
            }

            return notes;
        }

        private static void ParseLine(string line, List<Note> notes, double beatMs)
        {
            int i = 0;
            while (i < line.Length)
            {
                char c = line[i];

                // 跳过空格（用于判断时值）
                if (c == ' ' || c == '\t')
                {
                    i++;
                    continue;
                }

                // 跳过小节线等无关字符
                if (c == '|' || c == '[' || c == ']' || c == '(' || c == ')' || c == '/')
                {
                    i++;
                    continue;
                }

                // 休止符
                if (c == '0')
                {
                    double duration = ParseDuration(line, ref i, beatMs, true);
                    notes.Add(new Note { Key = "", Duration = (int)duration });
                    continue;
                }

                // 检查是否是音符
                if (IsNoteChar(c))
                {
                    ParseNote(line, ref i, notes, beatMs);
                    continue;
                }

                // 跳过其他字符
                i++;
            }
        }

        private static bool IsNoteChar(char c)
        {
            return (c >= '1' && c <= '7') || c == ',' || 
                   c == 'z' || c == 'x' || c == 'c' || c == 'v' || 
                   c == 'b' || c == 'n' || c == 'm' ||
                   c == 'Z' || c == 'X' || c == 'C' || c == 'V' || 
                   c == 'B' || c == 'N' || c == 'M';
        }

        private static void ParseNote(string line, ref int i, List<Note> notes, double beatMs)
        {
            int start = i;
            bool isSharp = false;
            bool isFlat = false;
            bool isUpOctave = false;
            bool isDownOctave = false;

            // 解析前缀变音和八度标记
            while (i < line.Length)
            {
                char c = line[i];
                if (c == '#') { isSharp = true; i++; }
                else if (c == 'b' && i + 1 < line.Length && IsNoteChar(line[i + 1])) { isFlat = true; i++; }
                else if (c == '\'') { isUpOctave = true; i++; }
                else if (c == '.') { isDownOctave = true; i++; }
                else break;
            }

            // 获取音符字符
            if (i >= line.Length) return;
            char noteChar = line[i];

            // 尝试从数字映射获取键盘
            string? key = null;
            if (NumberKeyMap.TryGetValue(noteChar, out string? numKey))
            {
                key = numKey;
                // 数字后面的逗号表示高八度do
                if (i + 1 < line.Length && line[i + 1] == ',')
                {
                    isUpOctave = true;
                    i++;
                }
            }
            // 尝试从字母映射获取键盘
            else if (LetterKeyMap.TryGetValue(char.ToLower(noteChar), out string? letterKey))
            {
                key = letterKey;
                // 字母后面可以跟逗号表示高八度
                if (i + 1 < line.Length && line[i + 1] == ',')
                {
                    isUpOctave = true;
                    i++;
                }
            }
            // 单独的逗号表示高八度do
            else if (noteChar == ',')
            {
                key = "z";
                isUpOctave = true;
            }
            else
            {
                // 不是有效音符，回退
                i = start + 1;
                return;
            }

            i++;

            // 解析后缀变音和八度标记
            while (i < line.Length)
            {
                char c = line[i];
                if (c == '#') { isSharp = true; i++; }
                else if (c == 'b' && i + 1 < line.Length && IsNoteChar(line[i + 1])) { isFlat = true; i++; }
                else if (c == '\'') { isUpOctave = true; i++; }
                else if (c == '.') { isDownOctave = true; i++; }
                else break;
            }

            // 解析时值
            double duration = ParseDuration(line, ref i, beatMs, false);

            // 创建音符
            var note = new Note
            {
                Key = key,
                Duration = (int)duration,
                IsSharp = isSharp,
                IsFlat = isFlat,
                IsNatural = !isSharp && !isFlat
            };

            notes.Add(note);
        }

        private static double ParseDuration(string line, ref int i, double beatMs, bool isRest)
        {
            double beats = 1.0;  // 默认1拍

            // 检查是否有十六分音符标记 ~
            bool isSixteenth = false;
            if (i < line.Length && line[i] == '~')
            {
                isSixteenth = true;
                i++;
            }

            // 如果是十六分音符，直接返回0.25拍
            if (isSixteenth)
            {
                return 0.25 * beatMs;
            }

            // 统计空格数量判断时值
            int spaceCount = 0;
            int j = i;
            while (j < line.Length && line[j] == ' ')
            {
                spaceCount++;
                j++;
            }

            // 根据空格数量确定基本时值
            if (spaceCount == 0)
            {
                // 没有空格：八分音符，0.5拍
                // 但需要检查下一个字符是否是音符或结束
                if (j < line.Length && IsNoteChar(line[j]))
                {
                    beats = 0.5;
                }
                else
                {
                    // 如果是行尾或后面不是音符，可能是四分音符
                    beats = 1.0;
                }
            }
            else if (spaceCount == 1)
            {
                beats = 1.0;  // 四分音符
                i = j;
            }
            else if (spaceCount >= 2)
            {
                beats = 2.0;  // 二分音符
                i = j;
            }

            // 检查延音线 -
            while (i < line.Length && line[i] == '-')
            {
                beats += 1.0;
                i++;
            }

            // 检查附点 · （一个附点×1.5，两个附点×1.75）
            int dotCount = 0;
            while (i < line.Length && line[i] == '·' && dotCount < 2)
            {
                dotCount++;
                i++;
            }

            if (dotCount == 1)
            {
                beats *= 1.5;
            }
            else if (dotCount == 2)
            {
                beats *= 1.75;
            }

            return beats * beatMs;
        }

        /// <summary>
        /// 将音符列表转换为脚本文本
        /// </summary>
        public static string ToScript(List<Note> notes, int bpm = 120)
        {
            if (notes == null || notes.Count == 0) return string.Empty;

            if (bpm <= 0) bpm = 120;
            double beatMs = 60000.0 / bpm;

            var sb = new StringBuilder();
            sb.AppendLine($"# BPM={bpm}");
            sb.AppendLine();

            foreach (var note in notes)
            {
                if (string.IsNullOrEmpty(note.Key))
                {
                    // 休止符
                    sb.AppendLine($"rest {note.Duration}ms");
                }
                else
                {
                    // 获取八度标记
                    string octave = "normal";
                    if (note.IsSharp) octave = "up";
                    else if (note.IsFlat) octave = "down";

                    // 获取变音标记
                    string accidental = "natural";
                    if (note.IsSharp) accidental = "sharp";
                    else if (note.IsFlat) accidental = "flat";

                    sb.AppendLine($"press {note.Key} {note.Duration}ms {octave} {accidental}");
                }
            }

            return sb.ToString();
        }
    }
}

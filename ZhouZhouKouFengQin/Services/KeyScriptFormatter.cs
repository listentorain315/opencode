using System;
using System.Collections.Generic;
using System.Text;
using GameMusicPlayer.Models;

namespace GameMusicPlayer.Services
{
    /// <summary>
    /// 把歌曲转成可读的「按键脚本」：游戏按什么键、按住什么鼠标键、多长、什么音高。
    /// </summary>
    public static class KeyScriptFormatter
    {
        private static readonly string[] PitchNames =
            { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

        private static readonly string[] Solfege =
            { "Do", "", "Re", "", "Mi", "Fa", "", "Sol", "", "La", "", "Si" };

        public static string Format(Song? song)
        {
            if (song == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"《{song.Name}》  {song.Artist}");
            sb.AppendLine($"事件 {song.Notes.Count} 个 · 总长 {song.DurationDisplay}");
            sb.AppendLine(new string('─', 52));
            sb.AppendLine($"{"#",3}  {"按键",-14} {"时值",7}  {"音高",-10} {"时刻",9}");
            sb.AppendLine(new string('─', 52));

            int idx = 0;
            double t = 0;
            foreach (var n in song.Notes)
            {
                idx++;
                sb.AppendLine($"{idx,3}  {KeyLabel(n),-14} {n.Duration,5}ms  {PitchLabel(n),-10} {TimeLabel(t),9}");
                t += n.Duration;
            }

            sb.AppendLine(new string('─', 52));
            sb.AppendLine("图例: 左键=降八度  右键=升八度  中键+左=降半音  中键+右=升半音");
            return sb.ToString();
        }

        private static string KeyLabel(Note n)
        {
            if (string.IsNullOrEmpty(n.Key)) return "—（休止）";

            string key = n.Key == "," ? "," : n.Key.ToUpperInvariant();
            var mods = new List<string>();
            if (n.IsNatural) mods.Add("中键");
            if (n.IsSharp && !n.IsFlat) mods.Add("右键");
            if (n.IsFlat && !n.IsSharp) mods.Add("左键");

            string effect =
                n.IsNatural && n.IsSharp ? " ♯升半音" :
                n.IsNatural && n.IsFlat ? " ♭降半音" :
                n.IsSharp ? " ↑升八度" :
                n.IsFlat ? " ↓降八度" : "";

            string press = mods.Count == 0 ? key : string.Join("+", mods) + "+" + key;
            return press + effect;
        }

        private static string PitchLabel(Note n)
        {
            if (string.IsNullOrEmpty(n.Key)) return "休止";
            if (!PreviewSoundService.TryGetMidi(n.Key, n.IsSharp, n.IsFlat, n.IsNatural, out int midi))
                return "?";

            int pc = ((midi % 12) + 12) % 12;
            int octave = midi / 12 - 1;
            string sol = Solfege[pc];
            return sol.Length == 0 ? $"{PitchNames[pc]}{octave}" : $"{PitchNames[pc]}{octave} {sol}";
        }

        private static string TimeLabel(double ms)
        {
            int total = (int)ms;
            return $"{total / 60000}:{total % 60000 / 1000:00}.{total % 1000:000}";
        }
    }
}

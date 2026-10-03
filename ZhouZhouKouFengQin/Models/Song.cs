using Newtonsoft.Json;
using System.Collections.Generic;

namespace GameMusicPlayer.Models
{
    public class Song
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("artist")]
        public string Artist { get; set; } = "未知艺术家";

        [JsonProperty("bpm")]
        public int Bpm { get; set; } = 120;

        [JsonProperty("notes")]
        public List<Note> Notes { get; set; } = new List<Note>();

        [JsonIgnore]
        public string Id { get; set; } = string.Empty;

        [JsonIgnore]
        public string DisplayName => $"{Name} - {Artist}";

        [JsonIgnore]
        public string DurationDisplay
        {
            get
            {
                double totalSeconds = 0;
                foreach (var note in Notes)
                {
                    totalSeconds += note.Duration / 1000.0;
                }
                int minutes = (int)(totalSeconds / 60);
                int seconds = (int)(totalSeconds % 60);
                return $"{minutes:D2}:{seconds:D2}";
            }
        }
    }

    public class Note
    {
        [JsonProperty("key")]
        public string Key { get; set; } = string.Empty;

        [JsonProperty("duration")]
        public int Duration { get; set; } = 500;

        [JsonProperty("isSharp")]
        public bool IsSharp { get; set; } = false;

        [JsonProperty("isFlat")]
        public bool IsFlat { get; set; } = false;

        [JsonProperty("isNatural")]
        public bool IsNatural { get; set; } = false;
    }

    public class SongList
    {
        [JsonProperty("songs")]
        public List<SongInfo> Songs { get; set; } = new List<SongInfo>();
    }

    public class SongInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("artist")]
        public string Artist { get; set; } = "未知艺术家";

        [JsonProperty("fileName")]
        public string FileName { get; set; } = string.Empty;
    }
}
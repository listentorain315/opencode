using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameMusicPlayer.Models;

namespace GameMusicPlayer.Services
{
    public class SongStorage
    {
        private readonly string _songsDirectory;
        private readonly string _songListFile;

        public SongStorage()
        {
            _songsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "songs");
            _songListFile = Path.Combine(_songsDirectory, "songlist.json");

            if (!Directory.Exists(_songsDirectory))
            {
                Directory.CreateDirectory(_songsDirectory);
            }

            if (!File.Exists(_songListFile))
            {
                SaveSongList(new SongList());
            }
        }

        /// <summary>
        /// 获取所有歌曲列表
        /// </summary>
        public SongList GetSongList()
        {
            try
            {
                var json = File.ReadAllText(_songListFile);
                return JsonConvert.DeserializeObject<SongList>(json) ?? new SongList();
            }
            catch
            {
                return new SongList();
            }
        }

        /// <summary>
        /// 保存歌曲列表
        /// </summary>
        public void SaveSongList(SongList songList)
        {
            var json = JsonConvert.SerializeObject(songList, Formatting.Indented);
            File.WriteAllText(_songListFile, json);
        }

        /// <summary>
        /// 获取歌曲文件路径
        /// </summary>
        public string GetSongFilePath(string fileName)
        {
            return Path.Combine(_songsDirectory, fileName);
        }

        /// <summary>
        /// 加载歌曲
        /// </summary>
        public Song? LoadSong(string fileName)
        {
            try
            {
                var filePath = GetSongFilePath(fileName);
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Song>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存歌曲
        /// </summary>
        public void SaveSong(Song song, string fileName)
        {
            var filePath = GetSongFilePath(fileName);
            var json = JsonConvert.SerializeObject(song, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 添加歌曲到列表
        /// </summary>
        public void AddSongToList(SongInfo songInfo)
        {
            var songList = GetSongList();

            // 生成唯一ID
            if (string.IsNullOrEmpty(songInfo.Id))
            {
                songInfo.Id = Guid.NewGuid().ToString("N")[..8];
            }

            songList.Songs.Add(songInfo);
            SaveSongList(songList);
        }

        /// <summary>
        /// 从列表中删除歌曲
        /// </summary>
        public void RemoveSongFromList(string songId)
        {
            var songList = GetSongList();
            var song = songList.Songs.FirstOrDefault(s => s.Id == songId);
            if (song != null)
            {
                // 删除歌曲文件
                var filePath = GetSongFilePath(song.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                songList.Songs.Remove(song);
                SaveSongList(songList);
            }
        }

        #region 简谱解析

        /// <summary>解析简谱为音符列表</summary>
        public List<Note> ParseNotation(string input, int bpm = 120)
        {
            return NotationParser.Parse(input, bpm);
        }

        /// <summary>解析简谱并返回被忽略的标记数</summary>
        public List<Note> ParseNotationDetailed(string input, int bpm, out int ignoredMarks)
        {
            // 使用新的解析器
            var notes = NotationParser.Parse(input, bpm);
            ignoredMarks = 0;  // 新解析器会忽略无效字符
            return notes;
        }

        #endregion
    }
}

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GameMusicPlayer.Services;

namespace GameMusicPlayer.Views
{
    public partial class ImageRecognitionView : UserControl
    {
        private readonly MainWindow _shell;
        private string? _selectedImagePath;

        public ImageRecognitionView(MainWindow shell)
        {
            InitializeComponent();
            _shell = shell;
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择简谱图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImage(dialog.FileName);
            }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                _selectedImagePath = filePath;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage.Source = bitmap;
                UploadHint.Visibility = Visibility.Collapsed;
                RecognizeButton.IsEnabled = true;
                StatusText.Text = $"已加载图片: {Path.GetFileName(filePath)}";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearImage_Click(object sender, RoutedEventArgs e)
        {
            PreviewImage.Source = null;
            UploadHint.Visibility = Visibility.Visible;
            RecognizeButton.IsEnabled = false;
            _selectedImagePath = null;
            StatusText.Text = "等待上传图片…";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("SubTextBrush");
            ResultTextBox.Clear();
        }

        private void ImageBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string ext = Path.GetExtension(files[0]).ToLower();
                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
                    {
                        LoadImage(files[0]);
                    }
                    else
                    {
                        MessageBox.Show("请上传图片文件（JPG、PNG、BMP）", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void ImageBorder_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Recognize_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedImagePath))
            {
                MessageBox.Show("请先选择图片", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RecognizeButton.IsEnabled = false;
            StatusText.Text = "正在识别中，请稍候...";
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("AmberBrush");

            try
            {
                string result = await RecognizeNotationAsync(_selectedImagePath);
                ResultTextBox.Text = result;
                
                // 统计音符数量
                int noteCount = CountNotes(result);
                if (noteCount > 0)
                {
                    StatusText.Text = $"识别完成，共 {noteCount} 个音符";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
                }
                else
                {
                    StatusText.Text = "未识别到音符，请检查图片";
                    StatusText.Foreground = (System.Windows.Media.Brush)FindResource("AmberBrush");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "识别失败";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
                MessageBox.Show($"识别失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RecognizeButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 识别简谱图片
        /// 使用Windows自带OCR API识别图片中的简谱
        /// </summary>
        private async Task<string> RecognizeNotationAsync(string imagePath)
        {
            try
            {
                // 使用OCR服务识别图片
                string ocrResult = await NotationOcrService.RecognizeAsync(imagePath);
                
                if (string.IsNullOrWhiteSpace(ocrResult))
                {
                    return "# 未能识别到简谱内容\n# 请确保图片清晰且包含数字简谱";
                }
                
                // 添加识别结果头
                StringBuilder result = new StringBuilder();
                result.AppendLine("# AI识别结果");
                result.AppendLine("# OCR识别语言: " + NotationOcrService.GetAvailableLanguage());
                result.AppendLine("# 请根据实际图片内容检查并修正以下简谱");
                result.AppendLine("#");
                result.AppendLine();
                result.AppendLine(ocrResult);
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"# 识别失败: {ex.Message}\n# 请检查图片格式或重试";
            }
        }

        private int CountNotes(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            
            int count = 0;
            foreach (char c in text)
            {
                if (c >= '1' && c <= '7') count++;
            }
            return count;
        }

        private void CopyResult_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ResultTextBox.Text))
            {
                Clipboard.SetText(ResultTextBox.Text);
                StatusText.Text = "已复制到剪贴板";
                StatusText.Foreground = (System.Windows.Media.Brush)FindResource("AccentBrush");
            }
        }

        private void ImportToInput_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBox.Text))
            {
                MessageBox.Show("没有可导入的内容，请先识别图片", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 跳转到手动录入页面并填入识别结果
            _shell.Navigate("record");
            
            // 通过MainWindow获取RecordInputView并设置内容
            _shell.SetRecordInputText(ResultTextBox.Text);
        }
    }
}

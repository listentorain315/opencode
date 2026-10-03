using System;
using System.Globalization;
using System.Windows.Data;

namespace GameMusicPlayer.Converters
{
    /// <summary>滑条已填充宽度：(Value, Min, Max, ActualWidth) → 像素宽</summary>
    public class SliderFillConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4) return 0.0;
            double v = ToDouble(values[0]);
            double min = ToDouble(values[1]);
            double max = ToDouble(values[2]);
            double width = ToDouble(values[3]);
            if (double.IsNaN(width) || width <= 0 || max <= min) return 0.0;
            double ratio = (v - min) / (max - min);
            return Math.Max(0.0, Math.Min(width, ratio * width));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static double ToDouble(object o)
        {
            return o is double d ? d : System.Convert.ToDouble(o);
        }
    }
}

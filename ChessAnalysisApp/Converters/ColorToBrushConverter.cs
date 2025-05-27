using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ChessAnalysisApp.Models;

namespace ChessAnalysisApp.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PieceColor color)
            {
                return color == PieceColor.White ? Brushes.White : Brushes.Black;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


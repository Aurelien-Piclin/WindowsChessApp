using ChessAnalysisApp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace ChessAnalysisApp.Models
{
    public partial class BoardSquare : ObservableObject
    {
        [ObservableProperty]
        private Brush color = Brushes.White;
        [ObservableProperty]
        private Piece? piece;

        public string? ImagePath => Piece == null ? null : PieceImageHelper.GetImagePath(Piece.Type, Piece.Color);


        partial void OnPieceChanged(Piece? oldValue, Piece? newValue)
        {
            OnPropertyChanged(nameof(ImagePath));
        }
    }
}
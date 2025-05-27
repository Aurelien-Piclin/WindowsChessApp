using ChessAnalysisApp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChessAnalysisApp.Models
{
    public enum PieceType { King, Queen, Rook, Bishop, Knight, Pawn }
    public enum PieceColor { White, Black }

    public partial class Piece : ObservableObject
    {
        [ObservableProperty]
        private PieceType type;

        [ObservableProperty]
        private PieceColor color;

        public string ImagePath => PieceImageHelper.GetImagePath(Type, Color);


        partial void OnTypeChanged(PieceType oldValue, PieceType newValue)
        {
            OnPropertyChanged(nameof(ImagePath));
        }

        partial void OnColorChanged(PieceColor oldValue, PieceColor newValue)
        {
            OnPropertyChanged(nameof(ImagePath));
        }
    }
}

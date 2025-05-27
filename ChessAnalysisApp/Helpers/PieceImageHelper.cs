using ChessAnalysisApp.Models;

namespace ChessAnalysisApp.Helpers
{
    public static class PieceImageHelper
    {
        public static string GetImagePath(PieceType type, PieceColor color)
        {
            return $"/ChessAnalysisApp;component/Images/{color.ToString().ToLower()}_{type.ToString().ToLower()}.png";
        }
    }
}


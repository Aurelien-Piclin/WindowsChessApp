using ChessAnalysisApp.Helpers;
using ChessAnalysisApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class AvailablePiece : ObservableObject
{
    public PieceType Type { get; set; }
    public PieceColor Color { get; set; }

    [ObservableProperty]
    private int count = 1;

    public string ImagePath => PieceImageHelper.GetImagePath(Type, Color);

}


using ChessAnalysisApp.Models;
using ChessAnalysisApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessAnalysisApp.Views
{
    public partial class ChessEditorView : UserControl
    {

        public ChessEditorView()
        {
            InitializeComponent();
        }

        private void AvailablePiece_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var image = sender as FrameworkElement;
                if (image?.DataContext is AvailablePiece piece)
                {
                    // Start drag with the AvailablePiece object as data
                    DragDrop.DoDragDrop(image, piece, DragDropEffects.Copy);
                }
            }
        }

        private void Board_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(AvailablePiece)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent(typeof(Piece)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Board_Drop(object sender, DragEventArgs e)
        {
            var border = sender as FrameworkElement;
            if (border?.DataContext is BoardSquare targetSquare)
            {
                if (e.Data.GetDataPresent(typeof(AvailablePiece)))
                {
                    var droppedPiece = e.Data.GetData(typeof(AvailablePiece)) as AvailablePiece;
                    if (droppedPiece == null) return;

                    if (this.DataContext is BoardViewModel vm)
                    {
                        // Si case occupée, renvoyer la pièce au stock
                        if (targetSquare.Piece != null)
                        {
                            vm.AddAvailablePiece(targetSquare.Piece.Type, targetSquare.Piece.Color);
                        }

                        // Poser la pièce
                        targetSquare.Piece = new Piece
                        {
                            Type = droppedPiece.Type,
                            Color = droppedPiece.Color
                        };

                        // Retirer du stock
                        vm.RemoveAvailablePiece(droppedPiece.Type, droppedPiece.Color);
                    }
                }
                else if (e.Data.GetDataPresent(typeof(Piece)))
                {
                    var draggedPiece = e.Data.GetData(typeof(Piece)) as Piece;
                    if (draggedPiece == null) return;

                    if (this.DataContext is BoardViewModel vm)
                    {
                        var sourceSquare = vm.AllSquares.FirstOrDefault(s => s.Piece == draggedPiece);
                        if (sourceSquare == null) return;

                        // Si la cible a une pièce, la renvoyer au stock
                        if (targetSquare.Piece != null)
                        {
                            vm.AddAvailablePiece(targetSquare.Piece.Type, targetSquare.Piece.Color);
                        }

                        // Déplacer la pièce
                        targetSquare.Piece = draggedPiece;

                        // Vider la case source
                        sourceSquare.Piece = null;
                    }
                }
            }
        }


        private void BoardSquare_RightClick(object sender, MouseButtonEventArgs e)
        {
            var border = sender as FrameworkElement;
            if (border?.DataContext is BoardSquare square && square.Piece != null)
            {
                if (this.DataContext is BoardViewModel vm)
                {
                    // Remettre la pièce dans AvailablePieces
                    var piece = square.Piece;
                    vm.AddAvailablePiece(piece.Type, piece.Color);
                    // Retirer la pièce de la case
                    square.Piece = null;
                }
            }
        }

        private void BoardPiece_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var border = sender as Border;
                if (border?.DataContext is BoardSquare square && square.Piece != null)
                {
                    DragDrop.DoDragDrop(border, square.Piece, DragDropEffects.Move);
                    e.Handled = true;
                }
            }
        }

        private void GroupedPiece_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var image = sender as FrameworkElement;
                if (image?.DataContext is GroupedPiece grouped && grouped.Count > 0)
                {
                    var piece = new AvailablePiece
                    {
                        Type = grouped.Type,
                        Color = grouped.Color
                    };

                    DragDrop.DoDragDrop(image, piece, DragDropEffects.Copy);
                }
            }
        }

        public bool ValidateEnPassantInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input == "-")
                return true;

            if (input.Length != 2)
                return false;

            char file = input[0]; // colonne a-h
            char rank = input[1]; // rangée 1-8

            bool validFile = file >= 'a' && file <= 'h';
            bool validRank = rank == '3' || rank == '6'; // En passant ne peut être que sur rangée 3 ou 6

            return validFile && validRank;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = true;
        }

    }
}

using ChessAnalysisApp.Models;
using ChessAnalysisApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessAnalysisApp.ViewModels
{
    public partial class BoardViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<BoardSquare> allSquares;

        public ObservableCollection<AvailablePiece> AvailablePieces { get; } = new();

        public ObservableCollection<GroupedPiece> WhitePieces { get; } = new();
        public ObservableCollection<GroupedPiece> BlackPieces { get; } = new();

        private PieceColor currentTurn = PieceColor.White;
        public PieceColor CurrentTurn
        {
            get => currentTurn;
            set
            {
                if (SetProperty(ref currentTurn, value))
                {
                    OnPropertyChanged(nameof(IsWhiteTurn));
                    OnPropertyChanged(nameof(IsBlackTurn));
                }
            }
        }

        public bool IsWhiteTurn
        {
            get => CurrentTurn == PieceColor.White;
            set
            {
                if (value) CurrentTurn = PieceColor.White;
            }
        }

        public bool IsBlackTurn
        {
            get => CurrentTurn == PieceColor.Black;
            set
            {
                if (value) CurrentTurn = PieceColor.Black;
            }
        }

        // Droits de roque
        [ObservableProperty]
        private bool whiteCanCastleKingSide = true;

        [ObservableProperty]
        private bool whiteCanCastleQueenSide = true;

        [ObservableProperty]
        private bool blackCanCastleKingSide = true;

        [ObservableProperty]
        private bool blackCanCastleQueenSide = true;

        // Case en passant possible, null si aucune
        [ObservableProperty]
        private string enPassantInput = "-";

        public IRelayCommand AnalyzeCommand { get; }

        public ICommand ResetToStartingPositionCommand { get; }

        private string _bestMove;
        public string BestMove
        {
            get => _bestMove;
            set
            {
                _bestMove = value;
                OnPropertyChanged(nameof(BestMove));
            }
        }

        private double evaluationBarValue = 0.5;
        public double EvaluationBarValue
        {
            get => evaluationBarValue;
            set
            {
                evaluationBarValue = value;
                OnPropertyChanged(nameof(EvaluationBarValue));
                OnPropertyChanged(nameof(EvaluationWhiteHeight));
                OnPropertyChanged(nameof(EvaluationBlackHeight));
            }
        }

        public double EvaluationWhiteHeight => EvaluationBarValue * 200;
        public double EvaluationBlackHeight => (1 - EvaluationBarValue) * 200;

        private string evaluationText;
        public string EvaluationText
        {
            get => evaluationText;
            set
            {
                evaluationText = value;
                OnPropertyChanged(nameof(EvaluationText));
            }
        }

        private string evaluationTextColor = "White";
        public string EvaluationTextColor
        {
            get => evaluationTextColor;
            set
            {
                evaluationTextColor = value;
                OnPropertyChanged(nameof(EvaluationTextColor));
            }
        }


        public BoardViewModel()
        {
            AllSquares = new ObservableCollection<BoardSquare>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var isDark = (row + col) % 2 == 1;
                    AllSquares.Add(new BoardSquare
                    {
                        Color = isDark
                                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A76E37"))
                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FADD90")),
                        Piece = null
                    });
                }
            }

            AnalyzeCommand = new RelayCommand(AnalyzePosition);
            ResetToStartingPositionCommand = new RelayCommand(ResetToStartingPosition);

            LoadAvailablePieces();
            UpdateGroupedPieces();

        }

        private void LoadAvailablePieces()
        {
            AvailablePieces.Clear();

            foreach (var color in new[] { PieceColor.White, PieceColor.Black })
            {
                for (int i = 0; i < 1; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.King, Color = color });
                for (int i = 0; i < 1; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.Queen, Color = color });
                for (int i = 0; i < 2; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.Rook, Color = color });
                for (int i = 0; i < 2; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.Bishop, Color = color });
                for (int i = 0; i < 2; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.Knight, Color = color });
                for (int i = 0; i < 8; i++)
                    AvailablePieces.Add(new AvailablePiece { Type = PieceType.Pawn, Color = color });
            }
        }

        public void UpdateGroupedPieces()
        {
            WhitePieces.Clear();
            BlackPieces.Clear();

            var grouped = AvailablePieces.GroupBy(p => new { p.Type, p.Color });

            foreach (var group in grouped)
            {
                var count = group.Count();
                var first = group.First();

                var groupedPiece = new GroupedPiece
                {
                    Type = first.Type,
                    Color = first.Color,
                    Count = count
                };

                if (groupedPiece.Color == PieceColor.White)
                    WhitePieces.Add(groupedPiece);
                else
                    BlackPieces.Add(groupedPiece);
            }
        }

        public void AddAvailablePiece(PieceType type, PieceColor color)
        {
            AvailablePieces.Add(new AvailablePiece { Type = type, Color = color });
            UpdateGroupedPieces();
        }

        public void RemoveAvailablePiece(PieceType type, PieceColor color)
        {
            var pieceToRemove = AvailablePieces.FirstOrDefault(p => p.Type == type && p.Color == color);
            if (pieceToRemove != null)
            {
                AvailablePieces.Remove(pieceToRemove);
                UpdateGroupedPieces();
            }
        }

        private async void AnalyzePosition()
        {
            if (!ValidateEnPassantInput(EnPassantInput))
            {
                MessageBox.Show("Case d'en passant invalide.\nElle doit être '-' ou une case valide entre a3-h3 ou a6-h6.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string fen = GenerateSimpleFEN();

            try
            {
                using var stockfish = new StockfishService();
                await stockfish.StartAsync();

                await stockfish.SendCommandAndReadUntilAsync("uci", "uciok");

                var (bestMove, evalText, evalBarValue, evalTextColor) = await stockfish.AnalyzePositionAsync(fen, IsWhiteTurn, 15);

                BestMove = bestMove;
                EvaluationText = evalText;
                EvaluationBarValue = evalBarValue;
                EvaluationTextColor = evalTextColor;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'analyse : Vérifier les droits de roques et la présence des 2 rois !",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public string GenerateSimpleFEN()
        {
            Dictionary<(PieceType, PieceColor), char> fenPieces = new()
    {
        { (PieceType.King, PieceColor.White), 'K' },
        { (PieceType.Queen, PieceColor.White), 'Q' },
        { (PieceType.Rook, PieceColor.White), 'R' },
        { (PieceType.Bishop, PieceColor.White), 'B' },
        { (PieceType.Knight, PieceColor.White), 'N' },
        { (PieceType.Pawn, PieceColor.White), 'P' },

        { (PieceType.King, PieceColor.Black), 'k' },
        { (PieceType.Queen, PieceColor.Black), 'q' },
        { (PieceType.Rook, PieceColor.Black), 'r' },
        { (PieceType.Bishop, PieceColor.Black), 'b' },
        { (PieceType.Knight, PieceColor.Black), 'n' },
        { (PieceType.Pawn, PieceColor.Black), 'p' }
    };

            var fenRows = new List<string>();

            for (int rank = 0; rank < 8; rank++)  // rangée 8 à 1 : rank=0->8ème rangée
            {
                int emptyCount = 0;
                var fenRow = new System.Text.StringBuilder();

                for (int file = 0; file < 8; file++)
                {
                    int index = rank * 8 + file;
                    var square = AllSquares[index];
                    if (square.Piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fenRow.Append(emptyCount);
                            emptyCount = 0;
                        }
                        fenRow.Append(fenPieces[(square.Piece.Type, square.Piece.Color)]);
                    }
                }

                if (emptyCount > 0)
                {
                    fenRow.Append(emptyCount);
                }

                fenRows.Add(fenRow.ToString());
            }

            string fen = string.Join("/", fenRows);
            string trait = CurrentTurn == PieceColor.White ? "w" : "b";

            // Non géré pour le moment
            string castling = GetCastlingRights();

            string enPassant = "-";

            return $"{fen} {trait} {castling} {enPassant} 0 1";
        }

        public string GetCastlingRights()
        {
            string rights = "";

            if (WhiteCanCastleKingSide) rights += "K";
            if (WhiteCanCastleQueenSide) rights += "Q";
            if (BlackCanCastleKingSide) rights += "k";
            if (BlackCanCastleQueenSide) rights += "q";

            return rights == "" ? "-" : rights;
        }

        private bool ValidateEnPassantInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input == "-")
                return true;

            if (input.Length != 2)
                return false;

            char file = input[0];
            char rank = input[1];

            bool validFile = file >= 'a' && file <= 'h';
            bool validRank = rank == '3' || rank == '6';

            return validFile && validRank;
        }

        public void ResetToStartingPosition()
        {
            // Vider le plateau
            foreach (var square in AllSquares)
            {
                square.Piece = null;
            }

            // Remettre la réserve complète (config de départ)
            LoadAvailablePieces();

            // Placement des pièces blanches (rangée 0 et 1)
            SetPieceAt(0, 0, PieceType.Rook, PieceColor.Black);
            SetPieceAt(0, 1, PieceType.Knight, PieceColor.Black);
            SetPieceAt(0, 2, PieceType.Bishop, PieceColor.Black);
            SetPieceAt(0, 3, PieceType.Queen, PieceColor.Black);
            SetPieceAt(0, 4, PieceType.King, PieceColor.Black);
            SetPieceAt(0, 5, PieceType.Bishop, PieceColor.Black);
            SetPieceAt(0, 6, PieceType.Knight, PieceColor.Black);
            SetPieceAt(0, 7, PieceType.Rook, PieceColor.Black);
            for (int col = 0; col < 8; col++)
            {
                SetPieceAt(1, col, PieceType.Pawn, PieceColor.Black);
            }

            // Placement des pièces noires (rangée 7 et 6)
            SetPieceAt(7, 0, PieceType.Rook, PieceColor.White);
            SetPieceAt(7, 1, PieceType.Knight, PieceColor.White);
            SetPieceAt(7, 2, PieceType.Bishop, PieceColor.White);
            SetPieceAt(7, 3, PieceType.Queen, PieceColor.White);
            SetPieceAt(7, 4, PieceType.King, PieceColor.White);
            SetPieceAt(7, 5, PieceType.Bishop, PieceColor.White);
            SetPieceAt(7, 6, PieceType.Knight, PieceColor.White);
            SetPieceAt(7, 7, PieceType.Rook, PieceColor.White);
            for (int col = 0; col < 8; col++)
            {
                SetPieceAt(6, col, PieceType.Pawn, PieceColor.White);
            }
        }

        private void SetPieceAt(int rank, int file, PieceType type, PieceColor color)
        {
            int index = rank * 8 + file;
            var square = AllSquares[index];

            // Placer la pièce sur la case
            square.Piece = new Piece { Type = type, Color = color };

            // Enlever la pièce correspondante de la réserve
            RemoveAvailablePiece(type, color);
        }

    }


}

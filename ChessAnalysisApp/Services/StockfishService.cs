using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace ChessAnalysisApp.Services
{
    public class StockfishService : IDisposable
    {
        private Process stockfishProcess;
        private StreamWriter input;
        private StreamReader output;


        public async Task StartAsync()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "stockfish.exe");

            if (!File.Exists(path))
                throw new FileNotFoundException("Stockfish introuvable", path);

            stockfishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            stockfishProcess.Start();
            input = stockfishProcess.StandardInput;
            output = stockfishProcess.StandardOutput;

            await SendCommandAndReadUntilAsync("uci", "uciok");
        }

        public void SendCommand(string command)
        {
            input.WriteLine(command);
            input.Flush();
        }

        public string ReadLine() => output.ReadLine();

        public void WaitFor(string expected)
        {
            string line;
            do
            {
                line = output.ReadLine();
                if (line == null)
                    throw new Exception($"Fin inattendue du flux sortie Stockfish, attendu : {expected}");
                Debug.WriteLine("[Stockfish] " + line);
            } while (!line.Contains(expected));
        }

        public void Dispose()
        {
            try
            {
                SendCommand("quit");
                stockfishProcess?.Kill();
                stockfishProcess?.Dispose();
            }
            catch { }
        }

        public async Task<string> SendCommandAndReadUntilAsync(string command, string expectedStart)
        {
            if (stockfishProcess == null || stockfishProcess.HasExited)
                throw new InvalidOperationException("Stockfish process is not running.");

            input.WriteLine(command);
            input.Flush();

            var result = new StringBuilder();
            string line;

            while ((line = await output.ReadLineAsync()) != null)
            {
                Debug.WriteLine("[Stockfish] " + line);
                result.AppendLine(line);
                if (line.StartsWith(expectedStart))
                    break;
            }

            return result.ToString();
        }

        public async Task<(string bestMove, string evalText, double evalBarValue, string evalTextColor)> AnalyzePositionAsync(string fen, bool isWhiteToMove, int depth = 15)
        {
            if (stockfishProcess == null || stockfishProcess.HasExited)
                throw new InvalidOperationException("Stockfish process is not running.");

            SendCommand($"position fen {fen}");
            await SendCommandAndReadUntilAsync("isready", "readyok");

            SendCommand($"go depth {depth}");

            string bestMove = null;
            string evaluationText = null;
            double evalBarValue = 0.5;
            string evalTextColor = "White";
            bool isInCheck = false;
            bool hasLegalMoves = false;


            string line;
            while ((line = await output.ReadLineAsync()) != null)
            {
                if (line.Contains("score cp"))
                {
                    string scoreStr = line.Split("score cp")[1].Trim().Split(' ')[0];
                    if (int.TryParse(scoreStr, out int cp))
                    {
                        evaluationText = $"{(cp >= 0 ? "+" : "")}{(cp / 100.0):0.0}";
                        evalBarValue = ConvertScoreToBarValue(cp, isWhiteToMove);
                        evalTextColor = cp >= 0 == isWhiteToMove ? "White" : "Black";
                    }
                }
                else if (line.Contains("score mate"))
                {
                    var parts = line.Split("score mate")[1].Trim().Split(' ');
                    if (int.TryParse(parts[0], out int mateIn))
                    {
                        bool isMateAgainst = line.Contains("score mate -");
                        evaluationText = $"M{Math.Abs(mateIn)}";
                        evalBarValue = isMateAgainst == isWhiteToMove ? 0.0 : 1.0;
                        evalTextColor = evalBarValue >= 0.5 ? "White" : "Black";

                    }
                }

                if (line.StartsWith("Legal moves:"))
                {
                    hasLegalMoves = !string.IsNullOrWhiteSpace(line.Replace("Legal moves:", "").Trim());
                }

                if (line.StartsWith("Checkers:"))
                {
                    isInCheck = !string.IsNullOrWhiteSpace(line.Replace("Checkers:", "").Trim());
                    break; // On a toutes les infos nécessaires
                }

                if (line.StartsWith("bestmove"))
                {
                    var parts = line.Split(' ');
                    if (parts[1] == "(none)")
                    {
                        bestMove = "Aucun";

                        // Demande un dump de la position pour savoir s'il y a échec ou non
                        SendCommand("d");
                        

                        while ((line = await output.ReadLineAsync()) != null)
                        {
                            if (line.StartsWith("Legal moves:"))
                                hasLegalMoves = !string.IsNullOrWhiteSpace(line.Replace("Legal moves:", "").Trim());

                            if (line.StartsWith("Checkers:"))
                            {
                                isInCheck = !string.IsNullOrWhiteSpace(line.Replace("Checkers:", "").Trim());
                                break;
                            }
                        }

                        if (!hasLegalMoves && isInCheck)
                        {
                            evaluationText = "Mat";
                            evalBarValue = isWhiteToMove ? 0.0 : 1.0;
                            evalTextColor = isWhiteToMove ? "Black" : "White";
                        }
                        else if (!hasLegalMoves && !isInCheck)
                        {
                            evaluationText = "Pat";
                            evalBarValue = 0.5;
                            evalTextColor = "Gray";
                        }
                    }
                    else
                    {
                        bestMove = parts[1];
                    }

                    break;
                }
            }

            return (bestMove, evaluationText ?? "0.0", evalBarValue, evalTextColor);
        }

        private double ConvertScoreToBarValue(int cp, bool isWhiteToMove)
        {
            const double maxCp = 1000.0;
            double adjusted = isWhiteToMove ? cp : -cp;
            return Math.Clamp((adjusted + maxCp) / (2 * maxCp), 0.0, 1.0);
        }


    }
}


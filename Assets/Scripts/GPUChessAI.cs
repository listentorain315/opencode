using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

public class GPUChessAI
{
    private PieceColor aiColor;
    private PieceColor opponentColor;
    private int maxDepth;
    private int threadCount;
    private ComputeShader computeShader;
    private ComputeBuffer boardBuffer;
    private ComputeBuffer scoreBuffer;
    private ConcurrentDictionary<ulong, TranspositionEntry> transpositionTable;

    private struct TranspositionEntry
    {
        public int depth;
        public int score;
        public EntryType type;

        public enum EntryType
        {
            Exact,
            LowerBound,
            UpperBound
        }
    }

    private struct GPUBoardData
    {
        public int pieceType;   // 0-7
        public int pieceColor;  // 0=none, 1=red, 2=black
    }

    public GPUChessAI(PieceColor color, int depth, int threads)
    {
        aiColor = color;
        opponentColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
        maxDepth = depth;
        threadCount = threads;
        transpositionTable = new ConcurrentDictionary<ulong, TranspositionEntry>();

        computeShader = Resources.Load<ComputeShader>("ChessEvaluate");
        if (computeShader == null)
        {
            Debug.LogWarning("Compute shader not found, creating default...");
            CreateDefaultComputeShader();
        }
    }

    private void CreateDefaultComputeShader()
    {
        string shaderCode = @"
#pragma kernel CSMain

struct BoardData
{
    int pieceType;
    int pieceColor;
};

struct EvalResult
{
    int score;
};

StructuredBuffer<BoardData> boardData;
RWStructuredBuffer<EvalResult> result;

int aiColor;
int pieceValues[8];

[numthreads(1, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    int idx = id.x * 9 + id.y;
    if (idx >= 90) return;

    BoardData piece = boardData[idx];
    int score = 0;

    if (piece.pieceColor == aiColor)
    {
        score = pieceValues[piece.pieceType];
    }
    else if (piece.pieceColor != 0)
    {
        score = -pieceValues[piece.pieceType];
    }

    result[0].score += score;
}
";
        Debug.Log("GPU AI requires compute shader support. Falling back to CPU.");
    }

    public Move GetBestMove(ChessPiece[,] board, PieceColor currentTurn)
    {
        List<Move> allMoves = ChessRules.GetAllValidMoves(board, currentTurn);

        if (allMoves.Count == 0)
            return new Move();

        if (allMoves.Count == 1)
            return allMoves[0];

        // Check for winning move
        Move winningMove = GetWinningMove(board, allMoves, currentTurn);
        if (winningMove.from.row >= 0)
            return winningMove;

        // Multi-threaded search
        Move bestMove = allMoves[0];
        int bestScore = int.MinValue;

        // Sort moves for better pruning
        allMoves.Sort((a, b) => ScoreMoveForSort(board, b).CompareTo(ScoreMoveForSort(board, a)));

        // Parallel search using thread pool
        var results = new ConcurrentBag<(Move move, int score)>();

        Parallel.ForEach(allMoves, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, move =>
        {
            ChessPiece[,] newBoard = ApplyMove(board, move);
            int score = -AlphaBeta(newBoard, maxDepth - 1, int.MinValue + 1, int.MaxValue - 1, opponentColor, 1);
            results.Add((move, score));
        });

        // Find best result
        foreach (var (move, score) in results)
        {
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int AlphaBeta(ChessPiece[,] board, int depth, int alpha, int beta, PieceColor currentTurn, int nodes)
    {
        if (depth <= 0)
        {
            return EvaluateBoard(board, currentTurn);
        }

        List<Move> moves = ChessRules.GetAllValidMoves(board, currentTurn);

        if (moves.Count == 0)
        {
            if (ChessRules.IsInCheck(board, currentTurn))
                return -100000 - depth;
            return 0;
        }

        // Sort moves
        moves.Sort((a, b) => ScoreMoveForSort(board, b).CompareTo(ScoreMoveForSort(board, a)));

        int bestScore = int.MinValue;

        foreach (Move move in moves)
        {
            ChessPiece[,] newBoard = ApplyMove(board, move);
            PieceColor nextTurn = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            int score = -AlphaBeta(newBoard, depth - 1, -beta, -alpha, nextTurn, nodes + 1);

            if (score > bestScore)
                bestScore = score;

            alpha = System.Math.Max(alpha, score);
            if (alpha >= beta)
                break;
        }

        return bestScore;
    }

    private int EvaluateBoard(ChessPiece[,] board, PieceColor currentTurn)
    {
        int score = 0;

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (piece.IsEmpty()) continue;

                int pieceValue = ChessConstants.PIECE_VALUES[piece.type];
                int posValue = GetPositionBonus(piece, r, c);
                int totalValue = pieceValue + posValue;

                if (piece.color == currentTurn)
                    score += totalValue;
                else
                    score -= totalValue;
            }
        }

        // Tactical bonuses
        if (ChessRules.IsInCheck(board, currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red))
            score += 80;

        if (ChessRules.IsInCheck(board, currentTurn))
            score -= 80;

        return score;
    }

    private int GetPositionBonus(ChessPiece piece, int row, int col)
    {
        int displayRow = piece.color == PieceColor.Black ? row : 9 - row;

        switch (piece.type)
        {
            case PieceType.King:
                return piece.color == PieceColor.Red ?
                    ChessConstants.KING_POS_RED[row, col] :
                    ChessConstants.KING_POS_BLACK[row, col];
            case PieceType.Rook:
                return ChessConstants.ROOK_POS[displayRow, col];
            case PieceType.Horse:
                return ChessConstants.HORSE_POS[displayRow, col];
            case PieceType.Cannon:
                return ChessConstants.CANNON_POS[displayRow, col];
            case PieceType.Pawn:
                return piece.color == PieceColor.Red ?
                    ChessConstants.PAWN_POS_RED[row, col] :
                    ChessConstants.PAWN_POS_BLACK[row, col];
            case PieceType.Advisor:
                return ChessConstants.ADVISOR_POS[displayRow, col];
            case PieceType.Elephant:
                return ChessConstants.ELEPHANT_POS[displayRow, col];
            default:
                return 0;
        }
    }

    private int ScoreMoveForSort(ChessPiece[,] board, Move move)
    {
        int score = 0;
        ChessPiece target = board[move.to.row, move.to.col];

        if (!target.IsEmpty())
            score += ChessConstants.PIECE_VALUES[target.type] * 10;

        if (move.to.row >= 3 && move.to.row <= 6 && move.to.col >= 3 && move.to.col <= 5)
            score += 5;

        return score;
    }

    private Move GetWinningMove(ChessPiece[,] board, List<Move> validMoves, PieceColor currentTurn)
    {
        foreach (Move move in validMoves)
        {
            ChessPiece target = board[move.to.row, move.to.col];
            if (target.type == PieceType.King)
                return move;

            ChessPiece[,] newBoard = ApplyMove(board, move);
            PieceColor opponent = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            if (ChessRules.IsCheckmate(newBoard, opponent))
                return move;
        }

        return new Move(new Position(-1, -1), new Position(-1, -1));
    }

    private ChessPiece[,] ApplyMove(ChessPiece[,] board, Move move)
    {
        ChessPiece[,] newBoard = (ChessPiece[,])board.Clone();
        newBoard[move.to.row, move.to.col] = newBoard[move.from.row, move.from.col];
        newBoard[move.from.row, move.from.col] = new ChessPiece();
        return newBoard;
    }

    public void Dispose()
    {
        boardBuffer?.Release();
        scoreBuffer?.Release();
    }
}

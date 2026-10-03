using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

public class AdvancedChessAI
{
    private PieceColor aiColor;
    private PieceColor opponentColor;
    private int maxDepth;
    private int threadCount;
    private ConcurrentDictionary<ulong, TranspositionEntry> transpositionTable;
    private ResourceManager resourceManager;
    private List<string> moveHistory;

    private struct TranspositionEntry
    {
        public int depth;
        public int score;
        public EntryType type;
        public Move bestMove;

        public enum EntryType
        {
            Exact,
            LowerBound,
            UpperBound
        }
    }

    private struct MoveEntry
    {
        public string moveStr;
        public int weight;

        public MoveEntry(string move, int weight)
        {
            this.moveStr = move;
            this.weight = weight;
        }
    }

    public AdvancedChessAI(PieceColor color, int depth, int threads = 8)
    {
        aiColor = color;
        opponentColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
        maxDepth = depth;
        threadCount = threads;
        transpositionTable = new ConcurrentDictionary<ulong, TranspositionEntry>();
        moveHistory = new List<string>();
        
        // 获取ResourceManager引用
        resourceManager = ResourceManager.Instance;
    }

    public Move GetBestMove(ChessPiece[,] board, PieceColor currentTurn)
    {
        transpositionTable.Clear();

        List<Move> allMoves = ChessRules.GetAllValidMoves(board, currentTurn);

        if (allMoves.Count == 0)
            return new Move();

        if (allMoves.Count == 1)
            return allMoves[0];

        // 检查外部开局库
        Move externalBookMove = QueryExternalOpeningBook(board, currentTurn, allMoves);
        if (externalBookMove.from.row >= 0)
            return externalBookMove;

        // 检查内置开局库
        Move openingMove = GetOpeningMove(board, currentTurn, allMoves);
        if (openingMove.from.row >= 0)
            return openingMove;

        // 检查获胜走法
        Move winningMove = GetWinningMove(board, allMoves, currentTurn);
        if (winningMove.from.row >= 0)
            return winningMove;

        // 多线程搜索
        Move bestMove = allMoves[0];
        int bestScore = int.MinValue;

        allMoves.Sort((a, b) => ScoreMove(board, b, maxDepth).CompareTo(ScoreMove(board, a, maxDepth)));

        var results = new ConcurrentBag<(Move move, int score)>();

        Parallel.ForEach(allMoves, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, move =>
        {
            ChessPiece[,] newBoard = ApplyMove(board, move);
            int score = -AlphaBeta(newBoard, maxDepth - 1, int.MinValue + 1, int.MaxValue - 1, opponentColor);
            results.Add((move, score));
        });

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

    // 查询外部开局库
    private Move QueryExternalOpeningBook(ChessPiece[,] board, PieceColor currentTurn, List<Move> validMoves)
    {
        if (resourceManager == null)
            return new Move(new Position(-1, -1), new Position(-1, -1));

        // 直接传递board给ResourceManager查询
        string bestMoveStr = resourceManager.QueryOpeningBook(board);

        if (string.IsNullOrEmpty(bestMoveStr))
            return new Move(new Position(-1, -1), new Position(-1, -1));

        // 解析走法
        Position from = ParsePosition(bestMoveStr.Substring(0, 2));
        Position to = ParsePosition(bestMoveStr.Substring(2, 2));

        Move move = new Move(from, to);

        // 验证走法是否有效
        if (validMoves.Any(m => m.from == move.from && m.to == move.to))
        {
            Debug.Log($"Using external opening book move: {bestMoveStr}");
            return move;
        }

        return new Move(new Position(-1, -1), new Position(-1, -1));
    }

    private int AlphaBeta(ChessPiece[,] board, int depth, int alpha, int beta, PieceColor currentTurn)
    {
        ulong hash = ComputeHash(board);

        if (transpositionTable.TryGetValue(hash, out TranspositionEntry entry))
        {
            if (entry.depth >= depth)
            {
                if (entry.type == TranspositionEntry.EntryType.Exact)
                    return entry.score;
                if (entry.type == TranspositionEntry.EntryType.LowerBound && entry.score >= beta)
                    return entry.score;
                if (entry.type == TranspositionEntry.EntryType.UpperBound && entry.score <= alpha)
                    return entry.score;
            }
        }

        if (depth <= 0)
            return QuiescenceSearch(board, alpha, beta, currentTurn, 4);

        List<Move> moves = ChessRules.GetAllValidMoves(board, currentTurn);

        if (moves.Count == 0)
        {
            if (ChessRules.IsInCheck(board, currentTurn))
                return -100000 - depth;
            return 0;
        }

        moves.Sort((a, b) => ScoreMove(board, b, depth).CompareTo(ScoreMove(board, a, depth)));

        Move bestMove = moves[0];
        int bestScore = int.MinValue;

        foreach (Move move in moves)
        {
            ChessPiece[,] newBoard = ApplyMove(board, move);
            PieceColor nextTurn = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            int score = -AlphaBeta(newBoard, depth - 1, -beta, -alpha, nextTurn);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = System.Math.Max(alpha, score);
            if (alpha >= beta)
                break;
        }

        transpositionTable[hash] = new TranspositionEntry
        {
            depth = depth,
            score = bestScore,
            type = bestScore <= alpha ? TranspositionEntry.EntryType.UpperBound :
                   bestScore >= beta ? TranspositionEntry.EntryType.LowerBound :
                   TranspositionEntry.EntryType.Exact,
            bestMove = bestMove
        };

        return bestScore;
    }

    private int QuiescenceSearch(ChessPiece[,] board, int alpha, int beta, PieceColor currentTurn, int depth)
    {
        int standPat = EvaluateBoard(board, currentTurn);

        if (depth <= 0)
            return standPat;

        if (standPat >= beta)
            return beta;

        if (standPat > alpha)
            alpha = standPat;

        List<Move> moves = ChessRules.GetAllValidMoves(board, currentTurn);
        List<Move> captureMoves = moves.Where(m => !board[m.to.row, m.to.col].IsEmpty()).ToList();

        captureMoves.Sort((a, b) => ScoreCaptureMove(board, b).CompareTo(ScoreCaptureMove(board, a)));

        foreach (Move move in captureMoves)
        {
            ChessPiece[,] newBoard = ApplyMove(board, move);
            PieceColor nextTurn = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            int score = -QuiescenceSearch(newBoard, -beta, -alpha, nextTurn, depth - 1);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    private int ScoreMove(ChessPiece[,] board, Move move, int depth)
    {
        int score = 0;

        ulong hash = ComputeHash(board);
        if (transpositionTable.TryGetValue(hash, out TranspositionEntry entry))
        {
            if (entry.bestMove.from == move.from && entry.bestMove.to == move.to)
                score += 10000;
        }

        ChessPiece target = board[move.to.row, move.to.col];
        if (!target.IsEmpty())
        {
            score += 5000 + ChessConstants.PIECE_VALUES[target.type] - ChessConstants.PIECE_VALUES[board[move.from.row, move.from.col].type] / 10;
        }

        ChessPiece[,] newBoard = ApplyMove(board, move);
        PieceColor nextColor = board[move.from.row, move.from.col].color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
        if (ChessRules.IsInCheck(newBoard, nextColor))
            score += 3000;

        if (move.to.row >= 3 && move.to.row <= 6 && move.to.col >= 3 && move.to.col <= 5)
            score += 50;

        return score;
    }

    private int ScoreCaptureMove(ChessPiece[,] board, Move move)
    {
        ChessPiece victim = board[move.to.row, move.to.col];
        ChessPiece attacker = board[move.from.row, move.from.col];
        return ChessConstants.PIECE_VALUES[victim.type] * 10 - ChessConstants.PIECE_VALUES[attacker.type];
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

                if (piece.color == currentTurn)
                    score += pieceValue + posValue;
                else
                    score -= pieceValue + posValue;
            }
        }

        score += EvaluateTactics(board, currentTurn);

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

    private int EvaluateTactics(ChessPiece[,] board, PieceColor currentTurn)
    {
        int score = 0;
        PieceColor opponent = currentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

        if (ChessRules.IsInCheck(board, opponent))
            score += 100;

        if (ChessRules.IsInCheck(board, currentTurn))
            score -= 100;

        if (ChessRules.IsCheckmate(board, opponent))
            score += 100000;

        if (ChessRules.IsCheckmate(board, currentTurn))
            score -= 100000;

        score += EvaluateBoardControl(board, currentTurn);
        score += EvaluatePieceCoordination(board, currentTurn);

        return score;
    }

    private int EvaluateBoardControl(ChessPiece[,] board, PieceColor currentTurn)
    {
        int score = 0;

        for (int r = 3; r <= 6; r++)
        {
            for (int c = 3; c <= 5; c++)
            {
                if (!board[r, c].IsEmpty())
                {
                    if (board[r, c].color == currentTurn)
                        score += 15;
                    else
                        score -= 15;
                }
            }
        }

        return score;
    }

    private int EvaluatePieceCoordination(ChessPiece[,] board, PieceColor currentTurn)
    {
        int score = 0;

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c].type == PieceType.Rook || board[r, c].type == PieceType.Horse)
                {
                    int mobility = CountMobility(board, r, c);
                    if (board[r, c].color == currentTurn)
                        score += mobility * 5;
                    else
                        score -= mobility * 5;
                }
            }
        }

        return score;
    }

    private int CountMobility(ChessPiece[,] board, int row, int col)
    {
        Position pos = new Position(row, col);
        List<Move> moves = ChessRules.GetValidMoves(board, pos, board[row, col].color);
        return moves.Count;
    }

    private Move GetOpeningMove(ChessPiece[,] board, PieceColor currentTurn, List<Move> validMoves)
    {
        // 构建局面键
        string positionKey = BuildPositionKey(board);
        
        // 查询内置开局库
        string bestMoveStr = BuiltInOpeningBook.GetOpeningMove(positionKey);
        
        if (!string.IsNullOrEmpty(bestMoveStr))
        {
            Position from = ParsePosition(bestMoveStr.Substring(0, 2));
            Position to = ParsePosition(bestMoveStr.Substring(2, 2));

            Move move = new Move(from, to);
            if (validMoves.Any(m => m.from == move.from && m.to == move.to))
            {
                Debug.Log($"Using opening book: {bestMoveStr} for position: {positionKey}");
                return move;
            }
        }

        return new Move(new Position(-1, -1), new Position(-1, -1));
    }

    private string BuildPositionKey(ChessPiece[,] board)
    {
        // 如果是初始局面
        if (moveHistory.Count == 0)
            return "start";

        // 根据走法历史构建局面键
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (string move in moveHistory)
        {
            if (sb.Length > 0)
                sb.Append("_");
            sb.Append(move);
        }
        return sb.ToString();
    }

    public void RecordMove(string move)
    {
        moveHistory.Add(move);
    }

    public void ResetHistory()
    {
        moveHistory.Clear();
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

    private ulong ComputeHash(ChessPiece[,] board)
    {
        ulong hash = 0;
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (!piece.IsEmpty())
                {
                    int pieceIndex = (int)piece.type * 2 + (piece.color == PieceColor.Red ? 0 : 1);
                    hash ^= ZobristTable[r, c, pieceIndex];
                }
            }
        }
        return hash;
    }

    private string GetBoardString(ChessPiece[,] board)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (piece.IsEmpty())
                    sb.Append('.');
                else
                {
                    char colorChar = piece.color == PieceColor.Red ? 'r' : 'b';
                    char typeChar = ' ';
                    switch (piece.type)
                    {
                        case PieceType.King: typeChar = 'k'; break;
                        case PieceType.Advisor: typeChar = 'a'; break;
                        case PieceType.Elephant: typeChar = 'e'; break;
                        case PieceType.Horse: typeChar = 'h'; break;
                        case PieceType.Rook: typeChar = 'r'; break;
                        case PieceType.Cannon: typeChar = 'c'; break;
                        case PieceType.Pawn: typeChar = 'p'; break;
                    }
                    sb.Append(colorChar);
                    sb.Append(typeChar);
                }
            }
        }
        return sb.ToString();
    }

    private Position ParsePosition(string pos)
    {
        int col = pos[0] - 'a';
        int row = 9 - (pos[1] - '0');
        return new Position(row, col);
    }

    private static readonly ulong[,,] ZobristTable = InitializeZobristTable();

    private static ulong[,,] InitializeZobristTable()
    {
        ulong[,,] table = new ulong[10, 9, 16];
        System.Random rng = new System.Random(12345);

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                for (int p = 0; p < 16; p++)
                {
                    byte[] bytes = new byte[8];
                    rng.NextBytes(bytes);
                    table[r, c, p] = System.BitConverter.ToUInt64(bytes, 0);
                }
            }
        }

        return table;
    }
}

using System.Collections.Generic;
using System.Linq;

public class ChessAI
{
    private PieceColor aiColor;
    private int maxDepth = 4;
    private int nodesSearched;
    private Dictionary<string, int> transpositionTable;

    // 开局库
    private static readonly Dictionary<string, List<string>> OPENING_BOOK = new Dictionary<string, List<string>>
    {
        // 中炮开局
        {"rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w", new List<string>{"h2e2", "b2e2"}},
        // 飞相局
        {"rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w", new List<string>{"c0e2", "g0e2"}},
        // 仙人指路
        {"rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w", new List<string>{"c3c4"}},
        // 飞象局
        {"rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w", new List<string>{"g2e2"}},
    };

    // 常见开局走法
    private static readonly string[] COMMON_OPENINGS_RED = {
        "h2e2", // 中炮
        "b2e2", // 中炮
        "c0e2", // 飞相
        "g0e2", // 飞相
        "c3c4", // 仙人指路
        "h0g2", // 跳马
        "b0c2", // 跳马
    };

    private static readonly string[] COMMON_OPENINGS_BLACK = {
        "h7e7", // 中炮
        "b7e7", // 中炮
        "c9e7", // 飞象
        "g9e7", // 飞象
        "c6c5", // 挺卒
        "h9g7", // 跳马
        "b9c7", // 跳马
    };

    public ChessAI(PieceColor color, int depth = 4)
    {
        aiColor = color;
        maxDepth = depth;
        transpositionTable = new Dictionary<string, int>();
    }

    public Move GetBestMove(ChessPiece[,] board)
    {
        nodesSearched = 0;
        transpositionTable.Clear();

        List<Move> allMoves = ChessRules.GetAllValidMoves(board, aiColor);

        if (allMoves.Count == 0)
            return new Move();

        // 开局阶段使用开局库
        if (IsOpeningPhase(board))
        {
            Move openingMove = GetOpeningMove(board, allMoves);
            if (openingMove.from.row >= 0)
                return openingMove;
        }

        // 使用Alpha-Beta剪枝搜索
        Move bestMove = allMoves[0];
        int bestScore = int.MinValue;

        // 对走法进行排序以提高剪枝效率
        allMoves.Sort((a, b) => EvaluateMove(board, b).CompareTo(EvaluateMove(board, a)));

        foreach (Move move in allMoves)
        {
            ChessPiece[,] newBoard = MakeMove(board, move);
            int score = AlphaBeta(newBoard, maxDepth - 1, int.MinValue, int.MaxValue, false);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int AlphaBeta(ChessPiece[,] board, int depth, int alpha, int beta, bool isMaximizing)
    {
        nodesSearched++;

        if (depth == 0)
            return EvaluateBoard(board);

        PieceColor currentColor = isMaximizing ? aiColor : (aiColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red);
        List<Move> moves = ChessRules.GetAllValidMoves(board, currentColor);

        if (moves.Count == 0)
        {
            if (ChessRules.IsInCheck(board, currentColor))
                return isMaximizing ? -100000 + (maxDepth - depth) : 100000 - (maxDepth - depth);
            return 0; // 和棋
        }

        // 对走法排序
        moves.Sort((a, b) => EvaluateMove(board, b).CompareTo(EvaluateMove(board, a)));

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (Move move in moves)
            {
                ChessPiece[,] newBoard = MakeMove(board, move);
                int eval = AlphaBeta(newBoard, depth - 1, alpha, beta, false);
                maxEval = System.Math.Max(maxEval, eval);
                alpha = System.Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in moves)
            {
                ChessPiece[,] newBoard = MakeMove(board, move);
                int eval = AlphaBeta(newBoard, depth - 1, alpha, beta, true);
                minEval = System.Math.Min(minEval, eval);
                beta = System.Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }

    private int EvaluateBoard(ChessPiece[,] board)
    {
        int score = 0;

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                ChessPiece piece = board[r, c];
                if (piece.IsEmpty()) continue;

                int pieceScore = ChessConstants.PIECE_VALUES[piece.type];
                int posScore = GetPositionValue(piece, r, c);

                if (piece.color == aiColor)
                    score += pieceScore + posScore;
                else
                    score -= pieceScore + posScore;
            }
        }

        // 加入一些战术评估
        score += EvaluateTactics(board);

        return score;
    }

    private int GetPositionValue(ChessPiece piece, int row, int col)
    {
        int displayRow = piece.color == PieceColor.Black ? row : 9 - row;

        switch (piece.type)
        {
            case PieceType.King:
                return piece.color == PieceColor.Red ? ChessConstants.KING_POS_RED[row, col] : ChessConstants.KING_POS_BLACK[row, col];
            case PieceType.Rook:
                return ChessConstants.ROOK_POS[displayRow, col];
            case PieceType.Horse:
                return ChessConstants.HORSE_POS[displayRow, col];
            case PieceType.Cannon:
                return ChessConstants.CANNON_POS[displayRow, col];
            case PieceType.Pawn:
                return piece.color == PieceColor.Red ? ChessConstants.PAWN_POS_RED[row, col] : ChessConstants.PAWN_POS_BLACK[row, col];
            case PieceType.Advisor:
                return ChessConstants.ADVISOR_POS[displayRow, col];
            case PieceType.Elephant:
                return ChessConstants.ELEPHANT_POS[displayRow, col];
            default:
                return 0;
        }
    }

    private int EvaluateTactics(ChessPiece[,] board)
    {
        int score = 0;

        // 检查将军威胁
        if (ChessRules.IsInCheck(board, aiColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red))
            score += 50;

        if (ChessRules.IsInCheck(board, aiColor))
            score -= 50;

        // 控制中心
        for (int r = 3; r <= 6; r++)
        {
            for (int c = 3; c <= 5; c++)
            {
                if (!board[r, c].IsEmpty())
                {
                    if (board[r, c].color == aiColor)
                        score += 10;
                    else
                        score -= 10;
                }
            }
        }

        return score;
    }

    private int EvaluateMove(ChessPiece[,] board, Move move)
    {
        int score = 0;
        ChessPiece target = board[move.to.row, move.to.col];

        // 吃子优先
        if (!target.IsEmpty())
        {
            score += ChessConstants.PIECE_VALUES[target.type] * 10;
        }

        // 占据中心
        if (move.to.row >= 3 && move.to.row <= 6 && move.to.col >= 3 && move.to.col <= 5)
        {
            score += 5;
        }

        return score;
    }

    private ChessPiece[,] MakeMove(ChessPiece[,] board, Move move)
    {
        ChessPiece[,] newBoard = (ChessPiece[,])board.Clone();
        newBoard[move.to.row, move.to.col] = newBoard[move.from.row, move.from.col];
        newBoard[move.from.row, move.from.col] = new ChessPiece();
        return newBoard;
    }

    private bool IsOpeningPhase(ChessPiece[,] board)
    {
        int pieceCount = 0;
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (!board[r, c].IsEmpty())
                    pieceCount++;
            }
        }
        return pieceCount >= 28; // 开局阶段棋子较多
    }

    private Move GetOpeningMove(ChessPiece[,] board, List<Move> validMoves)
    {
        string[] openings = aiColor == PieceColor.Red ? COMMON_OPENINGS_RED : COMMON_OPENINGS_BLACK;

        foreach (string opening in openings)
        {
            Position from = ParsePosition(opening.Substring(0, 2));
            Position to = ParsePosition(opening.Substring(2, 2));

            Move move = new Move(from, to);
            if (validMoves.Any(m => m.from == move.from && m.to == move.to))
            {
                return move;
            }
        }

        return new Move(new Position(-1, -1), new Position(-1, -1));
    }

    private Position ParsePosition(string pos)
    {
        int col = pos[0] - 'a';
        int row = 9 - (pos[1] - '0');
        return new Position(row, col);
    }
}

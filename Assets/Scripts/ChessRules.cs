using System.Collections.Generic;

public static class ChessRules
{
    public static bool IsValidMove(ChessPiece[,] board, Move move, PieceColor currentTurn)
    {
        ChessPiece piece = board[move.from.row, move.from.col];
        ChessPiece target = board[move.to.row, move.to.col];

        // 检查起始位置有棋子
        if (piece.IsEmpty())
            return false;

        // 检查是自己的棋子
        if (piece.color != currentTurn)
            return false;

        // 检查目标位置不是自己的棋子
        if (!target.IsEmpty() && target.color == piece.color)
            return false;

        // 检查目标位置是否有效
        if (!move.to.IsValid())
            return false;

        // 根据棋子类型检查走法
        switch (piece.type)
        {
            case PieceType.King:
                return IsValidKingMove(board, move, piece.color);
            case PieceType.Advisor:
                return IsValidAdvisorMove(board, move, piece.color);
            case PieceType.Elephant:
                return IsValidElephantMove(board, move, piece.color);
            case PieceType.Horse:
                return IsValidHorseMove(board, move);
            case PieceType.Rook:
                return IsValidRookMove(board, move);
            case PieceType.Cannon:
                return IsValidCannonMove(board, move);
            case PieceType.Pawn:
                return IsValidPawnMove(board, move, piece.color);
            default:
                return false;
        }
    }

    private static bool IsValidKingMove(ChessPiece[,] board, Move move, PieceColor color)
    {
        int rowDiff = System.Math.Abs(move.to.row - move.from.row);
        int colDiff = System.Math.Abs(move.to.col - move.from.col);

        // 将帅只能走一格，且只能直走
        if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
        {
            // 检查是否在九宫格内
            if (color == PieceColor.Red)
            {
                return move.to.row >= 7 && move.to.row <= 9 && move.to.col >= 3 && move.to.col <= 5;
            }
            else
            {
                return move.to.row >= 0 && move.to.row <= 2 && move.to.col >= 3 && move.to.col <= 5;
            }
        }

        // 将帅对面（飞将）
        if (rowDiff > 1 && colDiff == 0)
        {
            ChessPiece target = board[move.to.row, move.to.col];
            if (target.type == PieceType.King && target.color != color)
            {
                // 检查中间是否有棋子
                int minRow = System.Math.Min(move.from.row, move.to.row);
                int maxRow = System.Math.Max(move.from.row, move.to.row);
                for (int r = minRow + 1; r < maxRow; r++)
                {
                    if (!board[r, move.from.col].IsEmpty())
                        return false;
                }
                return true;
            }
        }

        return false;
    }

    private static bool IsValidAdvisorMove(ChessPiece[,] board, Move move, PieceColor color)
    {
        int rowDiff = System.Math.Abs(move.to.row - move.from.row);
        int colDiff = System.Math.Abs(move.to.col - move.from.col);

        // 士走斜线一格
        if (rowDiff == 1 && colDiff == 1)
        {
            // 检查是否在九宫格内
            if (color == PieceColor.Red)
            {
                return move.to.row >= 7 && move.to.row <= 9 && move.to.col >= 3 && move.to.col <= 5;
            }
            else
            {
                return move.to.row >= 0 && move.to.row <= 2 && move.to.col >= 3 && move.to.col <= 5;
            }
        }

        return false;
    }

    private static bool IsValidElephantMove(ChessPiece[,] board, Move move, PieceColor color)
    {
        int rowDiff = System.Math.Abs(move.to.row - move.from.row);
        int colDiff = System.Math.Abs(move.to.col - move.from.col);

        // 象走田字
        if (rowDiff == 2 && colDiff == 2)
        {
            // 检查象眼是否被堵
            int eyeRow = (move.from.row + move.to.row) / 2;
            int eyeCol = (move.from.col + move.to.col) / 2;
            if (!board[eyeRow, eyeCol].IsEmpty())
                return false;

            // 检查是否过河
            if (color == PieceColor.Red)
            {
                return move.to.row >= 5;
            }
            else
            {
                return move.to.row <= 4;
            }
        }

        return false;
    }

    private static bool IsValidHorseMove(ChessPiece[,] board, Move move)
    {
        int rowDiff = System.Math.Abs(move.to.row - move.from.row);
        int colDiff = System.Math.Abs(move.to.col - move.from.col);

        // 马走日字
        if ((rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2))
        {
            // 检查马腿是否被堵
            if (rowDiff == 2)
            {
                int legRow = move.from.row + (move.to.row > move.from.row ? 1 : -1);
                if (!board[legRow, move.from.col].IsEmpty())
                    return false;
            }
            else
            {
                int legCol = move.from.col + (move.to.col > move.from.col ? 1 : -1);
                if (!board[move.from.row, legCol].IsEmpty())
                    return false;
            }
            return true;
        }

        return false;
    }

    private static bool IsValidRookMove(ChessPiece[,] board, Move move)
    {
        // 车走直线
        if (move.from.row != move.to.row && move.from.col != move.to.col)
            return false;

        // 检查路径上是否有棋子
        if (move.from.row == move.to.row)
        {
            int minCol = System.Math.Min(move.from.col, move.to.col);
            int maxCol = System.Math.Max(move.from.col, move.to.col);
            for (int c = minCol + 1; c < maxCol; c++)
            {
                if (!board[move.from.row, c].IsEmpty())
                    return false;
            }
        }
        else
        {
            int minRow = System.Math.Min(move.from.row, move.to.row);
            int maxRow = System.Math.Max(move.from.row, move.to.row);
            for (int r = minRow + 1; r < maxRow; r++)
            {
                if (!board[r, move.from.col].IsEmpty())
                    return false;
            }
        }

        return true;
    }

    private static bool IsValidCannonMove(ChessPiece[,] board, Move move)
    {
        // 炮走直线
        if (move.from.row != move.to.row && move.from.col != move.to.col)
            return false;

        ChessPiece target = board[move.to.row, move.to.col];
        int pieceCount = 0;

        if (move.from.row == move.to.row)
        {
            int minCol = System.Math.Min(move.from.col, move.to.col);
            int maxCol = System.Math.Max(move.from.col, move.to.col);
            for (int c = minCol + 1; c < maxCol; c++)
            {
                if (!board[move.from.row, c].IsEmpty())
                    pieceCount++;
            }
        }
        else
        {
            int minRow = System.Math.Min(move.from.row, move.to.row);
            int maxRow = System.Math.Max(move.from.row, move.to.row);
            for (int r = minRow + 1; r < maxRow; r++)
            {
                if (!board[r, move.from.col].IsEmpty())
                    pieceCount++;
            }
        }

        // 不吃子时路径上不能有棋子
        if (target.IsEmpty())
            return pieceCount == 0;

        // 吃子时必须隔一个棋子
        return pieceCount == 1;
    }

    private static bool IsValidPawnMove(ChessPiece[,] board, Move move, PieceColor color)
    {
        int rowDiff = move.to.row - move.from.row;
        int colDiff = System.Math.Abs(move.to.col - move.from.col);

        // 兵只能走一格
        if (System.Math.Abs(rowDiff) + colDiff != 1)
            return false;

        if (color == PieceColor.Red)
        {
            // 红方兵未过河只能向前
            if (move.from.row >= 5 && rowDiff >= 0)
                return false;
            // 过河后可以左右走
            return true;
        }
        else
        {
            // 黑方兵未过河只能向前
            if (move.from.row <= 4 && rowDiff <= 0)
                return false;
            return true;
        }
    }

    public static bool IsInCheck(ChessPiece[,] board, PieceColor color)
    {
        // 找到将/帅的位置
        Position kingPos = new Position(-1, -1);
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c].type == PieceType.King && board[r, c].color == color)
                {
                    kingPos = new Position(r, c);
                    break;
                }
            }
            if (kingPos.row >= 0) break;
        }

        if (kingPos.row < 0) return true; // 将帅不存在，视为被将

        // 检查对方所有棋子是否能攻击到将帅
        PieceColor opponentColor = color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c].color == opponentColor)
                {
                    Move attackMove = new Move(new Position(r, c), kingPos);
                    if (IsValidMove(board, attackMove, opponentColor))
                        return true;
                }
            }
        }

        return false;
    }

    public static bool IsCheckmate(ChessPiece[,] board, PieceColor color)
    {
        // 尝试所有可能的走法
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c].color == color)
                {
                    List<Move> moves = GetValidMoves(board, new Position(r, c), color);
                    if (moves.Count > 0)
                        return false;
                }
            }
        }
        return true;
    }

    public static List<Move> GetValidMoves(ChessPiece[,] board, Position pos, PieceColor color)
    {
        List<Move> validMoves = new List<Move>();
        ChessPiece piece = board[pos.row, pos.col];

        if (piece.IsEmpty() || piece.color != color)
            return validMoves;

        // 生成所有可能的走法
        List<Move> possibleMoves = GeneratePossibleMoves(board, pos, piece);

        // 过滤掉会导致自己被将的走法
        foreach (Move move in possibleMoves)
        {
            ChessPiece[,] newBoard = (ChessPiece[,])board.Clone();
            newBoard[move.to.row, move.to.col] = newBoard[move.from.row, move.from.col];
            newBoard[move.from.row, move.from.col] = new ChessPiece();

            if (!IsInCheck(newBoard, color))
            {
                validMoves.Add(move);
            }
        }

        return validMoves;
    }

    private static List<Move> GeneratePossibleMoves(ChessPiece[,] board, Position pos, ChessPiece piece)
    {
        List<Move> moves = new List<Move>();

        switch (piece.type)
        {
            case PieceType.King:
                GenerateKingMoves(board, pos, piece.color, moves);
                break;
            case PieceType.Advisor:
                GenerateAdvisorMoves(board, pos, piece.color, moves);
                break;
            case PieceType.Elephant:
                GenerateElephantMoves(board, pos, piece.color, moves);
                break;
            case PieceType.Horse:
                GenerateHorseMoves(board, pos, moves);
                break;
            case PieceType.Rook:
                GenerateRookMoves(board, pos, moves);
                break;
            case PieceType.Cannon:
                GenerateCannonMoves(board, pos, moves);
                break;
            case PieceType.Pawn:
                GeneratePawnMoves(board, pos, piece.color, moves);
                break;
        }

        return moves;
    }

    private static void GenerateKingMoves(ChessPiece[,] board, Position pos, PieceColor color, List<Move> moves)
    {
        int[] dRows = { -1, 1, 0, 0 };
        int[] dCols = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            Position newPos = new Position(pos.row + dRows[i], pos.col + dCols[i]);
            if (newPos.IsValid())
            {
                if (color == PieceColor.Red && newPos.row >= 7 && newPos.row <= 9 && newPos.col >= 3 && newPos.col <= 5)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
                else if (color == PieceColor.Black && newPos.row >= 0 && newPos.row <= 2 && newPos.col >= 3 && newPos.col <= 5)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
            }
        }

        // 飞将
        int direction = color == PieceColor.Red ? -1 : 1;
        for (int r = pos.row + direction; r >= 0 && r <= 9; r += direction)
        {
            ChessPiece target = board[r, pos.col];
            if (!target.IsEmpty())
            {
                if (target.type == PieceType.King && target.color != color)
                    moves.Add(new Move(pos, new Position(r, pos.col)));
                break;
            }
        }
    }

    private static void GenerateAdvisorMoves(ChessPiece[,] board, Position pos, PieceColor color, List<Move> moves)
    {
        int[] dRows = { -1, -1, 1, 1 };
        int[] dCols = { -1, 1, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            Position newPos = new Position(pos.row + dRows[i], pos.col + dCols[i]);
            if (newPos.IsValid())
            {
                if (color == PieceColor.Red && newPos.row >= 7 && newPos.row <= 9 && newPos.col >= 3 && newPos.col <= 5)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
                else if (color == PieceColor.Black && newPos.row >= 0 && newPos.row <= 2 && newPos.col >= 3 && newPos.col <= 5)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
            }
        }
    }

    private static void GenerateElephantMoves(ChessPiece[,] board, Position pos, PieceColor color, List<Move> moves)
    {
        int[] dRows = { -2, -2, 2, 2 };
        int[] dCols = { -2, 2, -2, 2 };
        int[] eyeRows = { -1, -1, 1, 1 };
        int[] eyeCols = { -1, 1, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            Position newPos = new Position(pos.row + dRows[i], pos.col + dCols[i]);
            Position eyePos = new Position(pos.row + eyeRows[i], pos.col + eyeCols[i]);

            if (newPos.IsValid() && eyePos.IsValid())
            {
                // 检查象眼
                if (!board[eyePos.row, eyePos.col].IsEmpty())
                    continue;

                // 检查是否过河
                if (color == PieceColor.Red && newPos.row >= 5)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
                else if (color == PieceColor.Black && newPos.row <= 4)
                {
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
            }
        }
    }

    private static void GenerateHorseMoves(ChessPiece[,] board, Position pos, List<Move> moves)
    {
        int[] dRows = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] dCols = { -1, 1, -2, 2, -2, 2, -1, 1 };
        int[] legRows = { -1, -1, 0, 0, 0, 0, 1, 1 };
        int[] legCols = { 0, 0, -1, 1, -1, 1, 0, 0 };

        ChessPiece piece = board[pos.row, pos.col];

        for (int i = 0; i < 8; i++)
        {
            Position newPos = new Position(pos.row + dRows[i], pos.col + dCols[i]);
            Position legPos = new Position(pos.row + legRows[i], pos.col + legCols[i]);

            if (newPos.IsValid() && legPos.IsValid())
            {
                // 检查马腿
                if (!board[legPos.row, legPos.col].IsEmpty())
                    continue;

                ChessPiece target = board[newPos.row, newPos.col];
                if (target.IsEmpty() || target.color != piece.color)
                    moves.Add(new Move(pos, newPos));
            }
        }
    }

    private static void GenerateRookMoves(ChessPiece[,] board, Position pos, List<Move> moves)
    {
        ChessPiece piece = board[pos.row, pos.col];

        // 四个方向
        int[] dRows = { -1, 1, 0, 0 };
        int[] dCols = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            for (int step = 1; step <= 9; step++)
            {
                Position newPos = new Position(pos.row + dRows[i] * step, pos.col + dCols[i] * step);
                if (!newPos.IsValid()) break;

                ChessPiece target = board[newPos.row, newPos.col];
                if (target.IsEmpty())
                {
                    moves.Add(new Move(pos, newPos));
                }
                else
                {
                    if (target.color != piece.color)
                        moves.Add(new Move(pos, newPos));
                    break;
                }
            }
        }
    }

    private static void GenerateCannonMoves(ChessPiece[,] board, Position pos, List<Move> moves)
    {
        ChessPiece piece = board[pos.row, pos.col];

        int[] dRows = { -1, 1, 0, 0 };
        int[] dCols = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            bool foundPlatform = false;
            for (int step = 1; step <= 9; step++)
            {
                Position newPos = new Position(pos.row + dRows[i] * step, pos.col + dCols[i] * step);
                if (!newPos.IsValid()) break;

                ChessPiece target = board[newPos.row, newPos.col];
                if (!foundPlatform)
                {
                    if (target.IsEmpty())
                    {
                        moves.Add(new Move(pos, newPos));
                    }
                    else
                    {
                        foundPlatform = true;
                    }
                }
                else
                {
                    if (!target.IsEmpty())
                    {
                        if (target.color != piece.color)
                            moves.Add(new Move(pos, newPos));
                        break;
                    }
                }
            }
        }
    }

    private static void GeneratePawnMoves(ChessPiece[,] board, Position pos, PieceColor color, List<Move> moves)
    {
        ChessPiece piece = board[pos.row, pos.col];

        if (color == PieceColor.Red)
        {
            // 向前
            if (pos.row > 0)
            {
                Position newPos = new Position(pos.row - 1, pos.col);
                ChessPiece target = board[newPos.row, newPos.col];
                if (target.IsEmpty() || target.color != color)
                    moves.Add(new Move(pos, newPos));
            }

            // 过河后可以左右
            if (pos.row <= 4)
            {
                if (pos.col > 0)
                {
                    Position newPos = new Position(pos.row, pos.col - 1);
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
                if (pos.col < 8)
                {
                    Position newPos = new Position(pos.row, pos.col + 1);
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
            }
        }
        else
        {
            // 向前
            if (pos.row < 9)
            {
                Position newPos = new Position(pos.row + 1, pos.col);
                ChessPiece target = board[newPos.row, newPos.col];
                if (target.IsEmpty() || target.color != color)
                    moves.Add(new Move(pos, newPos));
            }

            // 过河后可以左右
            if (pos.row >= 5)
            {
                if (pos.col > 0)
                {
                    Position newPos = new Position(pos.row, pos.col - 1);
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
                if (pos.col < 8)
                {
                    Position newPos = new Position(pos.row, pos.col + 1);
                    ChessPiece target = board[newPos.row, newPos.col];
                    if (target.IsEmpty() || target.color != color)
                        moves.Add(new Move(pos, newPos));
                }
            }
        }
    }

    public static List<Move> GetAllValidMoves(ChessPiece[,] board, PieceColor color)
    {
        List<Move> allMoves = new List<Move>();

        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board[r, c].color == color)
                {
                    List<Move> moves = GetValidMoves(board, new Position(r, c), color);
                    allMoves.AddRange(moves);
                }
            }
        }

        return allMoves;
    }
}

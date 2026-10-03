using System.Collections.Generic;

public enum PieceType
{
    None = 0,
    King = 1,      // 将/帅
    Advisor = 2,   // 士/仕
    Elephant = 3,  // 象/相
    Horse = 4,     // 马
    Rook = 5,      // 车
    Cannon = 6,    // 炮
    Pawn = 7       // 卒/兵
}

public enum PieceColor
{
    None = 0,
    Red = 1,
    Black = 2
}

public struct Position
{
    public int row;  // 0-9
    public int col;  // 0-8

    public Position(int row, int col)
    {
        this.row = row;
        this.col = col;
    }

    public bool IsValid()
    {
        return row >= 0 && row <= 9 && col >= 0 && col <= 8;
    }

    public override bool Equals(object obj)
    {
        if (obj is Position other)
            return row == other.row && col == other.col;
        return false;
    }

    public override int GetHashCode()
    {
        return row * 9 + col;
    }

    public static bool operator ==(Position a, Position b)
    {
        return a.row == b.row && a.col == b.col;
    }

    public static bool operator !=(Position a, Position b)
    {
        return !(a == b);
    }
}

public struct ChessPiece
{
    public PieceType type;
    public PieceColor color;
    public Position position;
    public bool hasMoved;

    public ChessPiece(PieceType type, PieceColor color, Position position)
    {
        this.type = type;
        this.color = color;
        this.position = position;
        this.hasMoved = false;
    }

    public bool IsEmpty()
    {
        return type == PieceType.None;
    }

    public string GetDisplayName()
    {
        if (color == PieceColor.Red)
        {
            switch (type)
            {
                case PieceType.King: return "帅";
                case PieceType.Advisor: return "仕";
                case PieceType.Elephant: return "相";
                case PieceType.Horse: return "马";
                case PieceType.Rook: return "车";
                case PieceType.Cannon: return "炮";
                case PieceType.Pawn: return "兵";
                default: return "";
            }
        }
        else
        {
            switch (type)
            {
                case PieceType.King: return "将";
                case PieceType.Advisor: return "士";
                case PieceType.Elephant: return "象";
                case PieceType.Horse: return "马";
                case PieceType.Rook: return "车";
                case PieceType.Cannon: return "炮";
                case PieceType.Pawn: return "卒";
                default: return "";
            }
        }
    }
}

public struct Move
{
    public Position from;
    public Position to;
    public ChessPiece capturedPiece;
    public int score;

    public Move(Position from, Position to)
    {
        this.from = from;
        this.to = to;
        this.capturedPiece = new ChessPiece();
        this.score = 0;
    }

    public Move(Position from, Position to, int score)
    {
        this.from = from;
        this.to = to;
        this.capturedPiece = new ChessPiece();
        this.score = score;
    }
}

public static class ChessConstants
{
    public const int BOARD_ROWS = 10;
    public const int BOARD_COLS = 9;

    // 初始棋盘布局
    public static readonly PieceType[,] INITIAL_BOARD = new PieceType[,]
    {
        // 黑方 (上方)
        { PieceType.Rook, PieceType.Horse, PieceType.Elephant, PieceType.Advisor, PieceType.King, PieceType.Advisor, PieceType.Elephant, PieceType.Horse, PieceType.Rook },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.Cannon, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.Cannon, PieceType.None },
        { PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        // 红方 (下方)
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn, PieceType.None, PieceType.Pawn },
        { PieceType.None, PieceType.Cannon, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.Cannon, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.Rook, PieceType.Horse, PieceType.Elephant, PieceType.Advisor, PieceType.King, PieceType.Advisor, PieceType.Elephant, PieceType.Horse, PieceType.Rook }
    };

    // 棋子价值
    public static readonly Dictionary<PieceType, int> PIECE_VALUES = new Dictionary<PieceType, int>
    {
        { PieceType.King, 10000 },
        { PieceType.Rook, 600 },
        { PieceType.Cannon, 300 },
        { PieceType.Horse, 270 },
        { PieceType.Elephant, 120 },
        { PieceType.Advisor, 120 },
        { PieceType.Pawn, 30 }
    };

    // 位置价值表 - 红方视角（下方）
    public static readonly int[,] KING_POS_RED = new int[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 1, 1, 1, 0, 0, 0 },
        { 0, 0, 0, 2, 2, 2, 0, 0, 0 },
        { 0, 0, 0, 11, 15, 11, 0, 0, 0 }
    };

    public static readonly int[,] KING_POS_BLACK = new int[,]
    {
        { 0, 0, 0, 11, 15, 11, 0, 0, 0 },
        { 0, 0, 0, 2, 2, 2, 0, 0, 0 },
        { 0, 0, 0, 1, 1, 1, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 }
    };

    public static readonly int[,] ROOK_POS = new int[,]
    {
        { 6, 8, 6, 14, 12, 14, 6, 8, 6 },
        { 6, 8, 6, 14, 14, 14, 6, 8, 6 },
        { 6, 8, 6, 14, 14, 14, 6, 8, 6 },
        { 6, 10, 8, 14, 14, 14, 8, 10, 6 },
        { 6, 10, 8, 14, 14, 14, 8, 10, 6 },
        { 6, 10, 8, 14, 14, 14, 8, 10, 6 },
        { 6, 10, 8, 14, 14, 14, 8, 10, 6 },
        { 8, 12, 10, 16, 16, 16, 10, 12, 8 },
        { 12, 14, 12, 18, 18, 18, 12, 14, 12 },
        { 14, 16, 14, 20, 20, 20, 14, 16, 14 }
    };

    public static readonly int[,] HORSE_POS = new int[,]
    {
        { 4, 8, 16, 12, 4, 12, 16, 8, 4 },
        { 4, 10, 28, 16, 8, 16, 28, 10, 4 },
        { 12, 14, 16, 20, 18, 20, 16, 14, 12 },
        { 8, 24, 18, 24, 20, 24, 18, 24, 8 },
        { 6, 16, 14, 18, 16, 18, 14, 16, 6 },
        { 4, 12, 16, 14, 12, 14, 16, 12, 4 },
        { 2, 12, 12, 18, 16, 18, 12, 12, 2 },
        { 4, 8, 16, 16, 16, 16, 16, 8, 4 },
        { 0, 4, 8, 8, 8, 8, 8, 4, 0 },
        { 0, 2, 4, 4, 4, 4, 4, 2, 0 }
    };

    public static readonly int[,] CANNON_POS = new int[,]
    {
        { 4, 4, 0, 6, 14, 6, 0, 4, 4 },
        { 2, 2, 0, 6, 14, 6, 0, 2, 2 },
        { 2, 2, 0, 2, 6, 2, 0, 2, 2 },
        { 0, 0, 2, 4, 6, 4, 2, 0, 0 },
        { 0, 0, 0, 2, 4, 2, 0, 0, 0 },
        { 0, 0, 0, 2, 4, 2, 0, 0, 0 },
        { 0, 0, 2, 4, 6, 4, 2, 0, 0 },
        { 2, 2, 0, 2, 6, 2, 0, 2, 2 },
        { 2, 2, 0, 6, 14, 6, 0, 2, 2 },
        { 4, 4, 0, 6, 14, 6, 0, 4, 4 }
    };

    public static readonly int[,] PAWN_POS_RED = new int[,]
    {
        { 0, 0, 0, 2, 4, 2, 0, 0, 0 },
        { 4, 0, 8, 16, 16, 16, 8, 0, 4 },
        { 4, 0, 8, 14, 16, 14, 8, 0, 4 },
        { 0, 0, 2, 10, 14, 10, 2, 0, 0 },
        { 2, 0, 4, 6, 10, 6, 4, 0, 2 },
        { 0, 0, 0, 2, 6, 2, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 }
    };

    public static readonly int[,] PAWN_POS_BLACK = new int[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 2, 6, 2, 0, 0, 0 },
        { 2, 0, 4, 6, 10, 6, 4, 0, 2 },
        { 0, 0, 2, 10, 14, 10, 2, 0, 0 },
        { 4, 0, 8, 14, 16, 14, 8, 0, 4 },
        { 4, 0, 8, 16, 16, 16, 8, 0, 4 },
        { 0, 0, 0, 2, 4, 2, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 }
    };

    public static readonly int[,] ADVISOR_POS = new int[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 20, 0, 20, 0, 0, 0 },
        { 0, 0, 0, 0, 23, 0, 0, 0, 0 },
        { 0, 0, 0, 20, 0, 20, 0, 0, 0 }
    };

    public static readonly int[,] ELEPHANT_POS = new int[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 20, 0, 0, 0, 20, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 18, 0, 0, 0, 23, 0, 0, 0, 18 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 20, 0, 0, 0, 20, 0, 0 }
    };
}
